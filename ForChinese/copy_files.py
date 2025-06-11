import os
import shutil
import argparse
import re

def parse_macro_dependencies(config_file):
    """
    解析完整的C风格宏依赖配置文件
    支持: #ifdef, #ifndef, #elif, #else, #endif, #define
    """
    dependencies = {}  # 存储宏依赖关系: {base_macro: [dependent_macros]}
    
    if not os.path.exists(config_file):
        print(f"警告: 宏配置文件 {config_file} 不存在，使用空配置")
        return dependencies
    
    with open(config_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 匹配所有 #ifdef-#endif 块
    pattern = r'#ifdef\s+(\w+)\s*([\s\S]*?)#endif'
    matches = re.findall(pattern, content)
    
    for base_macro, block_content in matches:
        # 提取块内所有 #define 指令
        define_pattern = r'#define\s+(\w+)'
        dependent_macros = re.findall(define_pattern, block_content)
        
        if dependent_macros:
            dependencies[base_macro] = dependent_macros
    
    # 处理 #ifndef 块
    ifndef_pattern = r'#ifndef\s+(\w+)\s*([\s\S]*?)#endif'
    ifndef_matches = re.findall(ifndef_pattern, content)
    
    for base_macro, block_content in ifndef_matches:
        dependent_macros = re.findall(define_pattern, block_content)
        if dependent_macros:
            dependencies[base_macro] = dependent_macros
    
    # 处理 #elif 块 (简单版本，假设 #elif 直接跟在 #ifdef 后面)
    elif_pattern = r'#ifdef\s+(\w+)\s*([\s\S]*?)#elif\s+defined\(\s*(\w+)\s*\)\s*([\s\S]*?)(?=#else|#endif)'
    elif_matches = re.findall(elif_pattern, content)
    
    for _, _, elif_macro, elif_content in elif_matches:
        dependent_macros = re.findall(define_pattern, elif_content)
        if dependent_macros:
            dependencies.setdefault(elif_macro, []).extend(dependent_macros)
    
    # 处理 #else 块 (简单版本，假设 #else 直接跟在 #ifdef 后面)
    else_pattern = r'#ifdef\s+(\w+)\s*([\s\S]*?)#else\s*([\s\S]*?)#endif'
    else_matches = re.findall(else_pattern, content)
    
    for _, _, else_content in else_matches:
        dependent_macros = re.findall(define_pattern, else_content)
        if dependent_macros:
            dependencies.setdefault("__ELSE_BLOCK__", []).extend(dependent_macros)
    
    print(f"从配置文件解析了 {len(dependencies)} 个宏依赖关系")
    for base, deps in dependencies.items():
        print(f"  {base} -> {', '.join(deps)}")
    
    return dependencies

def resolve_macro_dependencies(base_macros, dependency_map):
    """
    解析宏依赖关系，返回所有需要启用的宏
    """
    enabled_macros = set(base_macros)
    stack = list(base_macros)
    
    while stack:
        current_macro = stack.pop()
        
        if current_macro in dependency_map:
            for dependent_macro in dependency_map[current_macro]:
                if dependent_macro not in enabled_macros:
                    enabled_macros.add(dependent_macro)
                    stack.append(dependent_macro)
    
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