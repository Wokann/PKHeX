import os
import shutil
import argparse
import re

def parse_macro_dependencies(config_file):
    """
    解析完整的C风格宏依赖配置文件
    使用严格的状态机准确跟踪条件块嵌套关系
    支持: #ifdef, #ifndef, #elif, #else, #endif, #define
    彻底解决条件组和 #elif 解析问题
    """
    dependencies = {
        'ifdef': {},
        'ifndef': {},
        'elif_else_groups': []
    }
    
    if not os.path.exists(config_file):
        print(f"警告: 宏配置文件 {config_file} 不存在，使用空配置")
        return dependencies
    
    with open(config_file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # 状态变量
    condition_stack = []       # 存储 (type, macro) 元组
    current_group = []         # 当前条件组 [ (type, macro, deps), ... ]
    current_block_type = None  # 当前块类型: ifdef, ifndef, elif, else
    current_macro = None       # 当前块的宏名
    current_deps = []          # 当前块的宏定义列表
    
    for line in lines:
        line = line.strip()
        if not line or line.startswith(('//', '/*')):
            continue
        
        # 保存当前块
        def save_current_block():
            nonlocal current_deps, current_block_type, current_macro
            if current_deps and current_block_type:
                current_group.append((current_block_type, current_macro, current_deps))
                current_deps = []
        
        # 处理 #ifdef
        if line.startswith('#ifdef'):
            save_current_block()
            parts = line.split()
            if len(parts) >= 2:
                current_macro = parts[1]
                current_block_type = 'ifdef'
                condition_stack.append(('ifdef', current_macro))
                current_group = []  # 开始新条件组
        
        # 处理 #ifndef
        elif line.startswith('#ifndef'):
            save_current_block()
            parts = line.split()
            if len(parts) >= 2:
                current_macro = parts[1]
                current_block_type = 'ifndef'
                condition_stack.append(('ifndef', current_macro))
                current_group = []
        
        # 处理 #elif
        elif line.startswith('#elif'):
            save_current_block()
            # 支持两种格式: #elif defined(MACRO) 和 #elif MACRO
            match = re.search(r'#elif\s+(?:defined\(\s*(\w+)\s*\)|(\w+))', line)
            if match:
                current_macro = match.group(1) or match.group(2)
                current_block_type = 'elif'
        
        # 处理 #else
        elif line.startswith('#else'):
            save_current_block()
            current_macro = None
            current_block_type = 'else'
        
        # 处理 #endif
        elif line.startswith('#endif'):
            save_current_block()
            if condition_stack:
                top_type, top_macro = condition_stack.pop()
                # 独立的 ifdef/ifndef 块
                if not condition_stack and len(current_group) == 1:
                    block_type, block_macro, deps = current_group[0]
                    if block_type in ('ifdef', 'ifndef'):
                        dependencies[block_type][block_macro] = deps
                        current_group = []
                # 条件组结束
                elif not condition_stack and current_group:
                    dependencies['elif_else_groups'].append(current_group)
                    current_group = []
            current_block_type = None
            current_macro = None
        
        # 收集 #define 指令
        defines = re.findall(r'#define\s+(\w+)', line)
        current_deps.extend(defines)
    
    # 处理最后一个块
    save_current_block()
    if current_group:
        dependencies['elif_else_groups'].append(current_group)
    
    # 输出解析结果
    print(f"从配置文件解析了 {len(dependencies['ifdef'])} 个 #ifdef 依赖关系")
    for base, deps in dependencies['ifdef'].items():
        print(f"  #ifdef {base} -> {', '.join(deps)}")
    
    print(f"从配置文件解析了 {len(dependencies['ifndef'])} 个 #ifndef 依赖关系")
    for base, deps in dependencies['ifndef'].items():
        print(f"  #ifndef {base} -> {', '.join(deps)}")
    
    print(f"从配置文件解析了 {len(dependencies['elif_else_groups'])} 个 #elif/#else 组")
    for i, group in enumerate(dependencies['elif_else_groups']):
        print(f"  组 {i+1}:")
        for cond_type, macro, deps in group:
            if cond_type == 'else':
                print(f"    #else -> {', '.join(deps)}")
            else:
                print(f"    #{cond_type} {macro} -> {', '.join(deps)}")
    
    return dependencies

def resolve_macro_dependencies(base_macros, dependency_map):
    """
    解析宏依赖关系，返回所有需要启用的宏
    正确处理 #ifdef 和 #ifndef 的不同语义
    正确处理 #elif 的互斥性
    """
    enabled_macros = set(base_macros)
    stack = list(base_macros)
    
    # 从配置文件中获取不同类型的依赖关系
    ifdef_deps = dependency_map.get('ifdef', {})
    ifndef_deps = dependency_map.get('ifndef', {})
    elif_else_groups = dependency_map.get('elif_else_groups', [])
    
    # 处理 #ifdef 依赖 - 如果宏被启用，则启用其依赖的宏
    while stack:
        current_macro = stack.pop()
        
        # 处理 #ifdef 依赖
        if current_macro in ifdef_deps:
            for dependent_macro in ifdef_deps[current_macro]:
                if dependent_macro not in enabled_macros:
                    enabled_macros.add(dependent_macro)
                    stack.append(dependent_macro)
    
    # 处理 #ifndef 依赖 - 如果宏未被启用，则启用其依赖的宏
    for macro, deps in ifndef_deps.items():
        if macro not in enabled_macros:  # 关键逻辑：如果宏未被启用
            for dep in deps:
                if dep not in enabled_macros:
                    enabled_macros.add(dep)
                    stack.append(dep)
    
    # 处理 #elif/#else 组 - 只启用第一个条件为真的块中的宏
    for group in elif_else_groups:
        condition_met = False
        
        for cond_type, macro, deps in group:
            if condition_met:
                continue  # 已经有条件为真，跳过后续条件
            
            if cond_type == 'ifdef':
                if macro in enabled_macros:
                    condition_met = True
                    for dep in deps:
                        if dep not in enabled_macros:
                            enabled_macros.add(dep)
                            stack.append(dep)
            
            elif cond_type == 'elif':
                if macro in enabled_macros:
                    condition_met = True
                    for dep in deps:
                        if dep not in enabled_macros:
                            enabled_macros.add(dep)
                            stack.append(dep)
            
            elif cond_type == 'else':
                # #else 总是在最后，并且只有在前面的条件都不满足时才会执行
                if not condition_met:
                    for dep in deps:
                        if dep not in enabled_macros:
                            enabled_macros.add(dep)
                            stack.append(dep)
    
    return list(enabled_macros)

def process_file_content(content, enabled_macros):
    """
    处理文件内容，支持宏的多重嵌套
    先识别普通宏标记，再识别特殊标记
    实现类似C语言的if-elseif-else条件判断逻辑
    严格保持原始字符格式和文件末尾格式不变
    """
    # 嵌套条件状态栈，每个元素为 (condition_met, should_keep_block)
    condition_stack = []
    processed_lines = []
    debug_output = []  # 调试信息收集列表
    
    # 宏标记正则表达式 - 使用字节模式，避免字符编码问题
    normal_macro_pattern = rb'^\s*\[(?!ELSE|END|ELSEIF)(\w+)\]\s*$'
    elseif_pattern = rb'^\s*\[ELSEIF (\w+)\]\s*$'
    else_pattern = rb'^\s*\[ELSE\]\s*$'
    end_pattern = rb'^\s*\[/END\]\s*$'
    
    # 处理完整的文件内容，逐行分析但不修改格式
    lines = content.splitlines(keepends=True)
    line_buffer = bytearray()
    last_line_kept = None  # 记录最后保留的行（用于处理末尾END标记）
    
    for i, line in enumerate(lines):
        is_last_line = (i == len(lines) - 1)
        
        # 检测行结束符（保留原始换行符）
        line_ending = b''
        if line.endswith(b'\r\n'):
            line_ending = b'\r\n'
        elif line.endswith(b'\n'):
            line_ending = b'\n'
        
        # 移除行结束符进行处理
        line_content = line[:-len(line_ending)] if line_ending else line
        
        # 普通宏标记处理
        normal_match = re.match(normal_macro_pattern, line_content)
        if normal_match:
            # 处理之前积累的行内容（代码正文）
            if line_buffer:
                # 代码行处理：仅当条件满足时保留行
                if not condition_stack or all(level[1] for level in condition_stack):
                    processed_lines.append(bytes(line_buffer))
                    last_line_kept = bytes(line_buffer)
                line_buffer = bytearray()
            
            # 仅对宏名进行解码
            macro_name = normal_match.group(1).decode('utf-8', errors='replace')
            is_enabled = macro_name in enabled_macros
            condition_stack.append((is_enabled, is_enabled))
            debug_output.append(f"[DEBUG] 普通宏: {macro_name} {'启用' if is_enabled else '未启用'}")
            continue
        
        # ELSEIF标记处理
        elseif_match = re.match(elseif_pattern, line_content)
        if elseif_match and condition_stack:
            # 处理之前积累的行内容
            if line_buffer:
                if not condition_stack or all(level[1] for level in condition_stack):
                    processed_lines.append(bytes(line_buffer))
                    last_line_kept = bytes(line_buffer)
                line_buffer = bytearray()
            
            # 仅对ELSEIF宏名进行解码
            elseif_macro = elseif_match.group(1).decode('utf-8', errors='replace')
            if not condition_stack[-1][0]:
                is_enabled = elseif_macro in enabled_macros
                condition_stack[-1] = (is_enabled, is_enabled)
                status = "启用" if is_enabled else "未启用"
            else:
                condition_stack[-1] = (condition_stack[-1][0], False)
                status = "跳过（前置条件满足）"
            debug_output.append(f"[DEBUG] ELSEIF: {elseif_macro} {status}")
            continue
        
        # ELSE标记处理
        else_match = re.match(else_pattern, line_content)
        if else_match and condition_stack:
            # 处理之前积累的行内容
            if line_buffer:
                if not condition_stack or all(level[1] for level in condition_stack):
                    processed_lines.append(bytes(line_buffer))
                    last_line_kept = bytes(line_buffer)
                line_buffer = bytearray()
            
            should_keep = not condition_stack[-1][0]
            condition_stack[-1] = (condition_stack[-1][0], should_keep)
            status = "启用" if should_keep else "跳过（前置条件满足）"
            debug_output.append(f"[DEBUG] ELSE {status}")
            continue
        
        # END标记处理
        end_match = re.match(end_pattern, line_content)
        if end_match and condition_stack:
            # 处理之前积累的行内容
            if line_buffer:
                if not condition_stack or all(level[1] for level in condition_stack):
                    processed_lines.append(bytes(line_buffer))
                    last_line_kept = bytes(line_buffer)
                line_buffer = bytearray()
            
            # 特殊处理：如果是最后一行且没有换行符
            if is_last_line and not line_ending:
                # 删除最后保留行的换行符
                if last_line_kept and processed_lines:
                    # 检查最后保留的行是否有换行符
                    if last_line_kept.endswith(b'\r\n'):
                        processed_lines[-1] = processed_lines[-1][:-2]  # 删除\r\n
                    elif last_line_kept.endswith(b'\n'):
                        processed_lines[-1] = processed_lines[-1][:-1]  # 删除\n
            
            condition_stack.pop()
            debug_output.append(f"[DEBUG] 宏块结束（当前层级: {len(condition_stack)}）")
            continue
        
        # 代码行处理：累积字节数据，不做任何字符转换
        line_buffer.extend(line)
    
    # 处理最后一行
    if line_buffer:
        # 代码行处理：仅当条件满足时保留行
        if not condition_stack or all(level[1] for level in condition_stack):
            processed_lines.append(bytes(line_buffer))
    
    # 输出调试信息
    for debug in debug_output:
        print(debug)
    
    return b''.join(processed_lines)

def copy_files(source_folder, destination_folder, enabled_macros=None):
    """递归复制文件夹并处理宏定义，严格保持原始换行符格式"""
    if not os.path.exists(destination_folder):
        os.makedirs(destination_folder)
        print(f"创建目标文件夹: {destination_folder}")
    
    items = os.listdir(source_folder)
    total = len(items)
    processed = 0
    print(f"开始处理 {total} 个文件/文件夹")
    
    for item in items:
        src_path = os.path.join(source_folder, item)
        dst_path = os.path.join(destination_folder, item)
        
        try:
            if os.path.isfile(src_path):
                with open(src_path, 'rb') as f:
                    content = f.read()
                
                if enabled_macros:
                    processed_content = process_file_content(content, enabled_macros)
                    with open(dst_path, 'wb') as f:
                        f.write(processed_content)
                else:
                    shutil.copy2(src_path, dst_path)
                
                processed += 1
                print(f"复制文件: {item}")
            
            elif os.path.isdir(src_path):
                copy_files(src_path, dst_path, enabled_macros)
                processed += 1
                print(f"处理文件夹: {item}")
                
        except Exception as e:
            print(f"处理 {item} 出错: {str(e)}")
    
    print(f"处理完成: 成功处理 {processed} 个项目")

def main():
    parser = argparse.ArgumentParser(description="宏处理工具（支持完整C风格条件编译指令）")
    parser.add_argument("source", help="源文件夹路径")
    parser.add_argument("destination", help="目标文件夹路径")
    parser.add_argument("-m", "--macros", nargs="+", help="启用的宏定义列表")
    parser.add_argument("-c", "--config", help="宏依赖配置文件路径")
    args = parser.parse_args()
    
    if not os.path.exists(args.source):
        print(f"错误: 源文件夹 {args.source} 不存在")
        return
    
    # 从命令行参数获取基础宏
    base_macros = args.macros or []
    
    # 从配置文件解析宏依赖关系
    dependency_map = {}
    if args.config:
        dependency_map = parse_macro_dependencies(args.config)
    
    # 解析所有需要启用的宏（包括依赖项）
    enabled_macros = resolve_macro_dependencies(base_macros, dependency_map)
    
    print(f"最终启用的宏: {', '.join(enabled_macros)}")
    
    copy_files(args.source, args.destination, enabled_macros)

if __name__ == "__main__":
    main()
