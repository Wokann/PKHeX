﻿using System;
using System.Collections;
using System.Windows.Forms;

namespace PKHeX
{
    public partial class SAV_PokedexXY : Form
    {
        public SAV_PokedexXY()
        {
            InitializeComponent();
            CP = new[] { CHK_P1, CHK_P2, CHK_P3, CHK_P4, CHK_P5, CHK_P6, CHK_P7, CHK_P8, CHK_P9, };
            CL = new[] { CHK_L1, CHK_L2, CHK_L3, CHK_L4, CHK_L5, CHK_L6, CHK_L7, };
            Util.TranslateInterface(this, Main.curlanguage);
            sav = (byte[])Main.SAV.Data.Clone();

            Setup();
            editing = false;
            LB_Species.SelectedIndex = 0;
            TB_Spinda.Text = BitConverter.ToUInt32(sav, Main.SAV.Spinda).ToString("X8");
        }

        private CheckBox[] CP, CL;
        public byte[] sav;
        public bool[,] specbools = new bool[9, 0x60 * 8];
        public bool[,] langbools = new bool[7, 0x60 * 8];
        public bool[] foreignbools = new bool[0x52 * 8];
        bool editing = true;
        private void Setup()
        {
            // Clear Listbox and ComboBox
            LB_Species.Items.Clear();
            CB_Species.Items.Clear();

            // Fill List
            #region Species
            {
                var species_list = Util.getCBList(Main.specieslist, null);
                species_list.RemoveAt(0); // Remove 0th Entry
                CB_Species.DisplayMember = "Text";
                CB_Species.ValueMember = "Value";
                CB_Species.DataSource = species_list;
            }
            #endregion

            for (int i = 1; i < Main.specieslist.Length; i++)
                LB_Species.Items.Add(i.ToString("000") + " - " + Main.specieslist[i]);

            getBools();
        }
        private void changeCBSpecies(object sender, EventArgs e)
        {
            if (editing) return;
            setBools();

            editing = true;
            species = (int)CB_Species.SelectedValue;
            LB_Species.SelectedIndex = species - 1; // Since we don't allow index0 in combobox, everything is shifted by 1
            LB_Species.TopIndex = LB_Species.SelectedIndex;
            loadchks();
            editing = false;
        }
        private void changeLBSpecies(object sender, EventArgs e)
        {
            if (editing) return;
            setBools();

            editing = true;
            species = LB_Species.SelectedIndex + 1;
            CB_Species.SelectedValue = species;
            loadchks();
            editing = false;
        }
        private void loadchks()
        {
            // Load Bools for the data
            int pk = species;

            L_Spinda.Visible = TB_Spinda.Visible = pk == 327;

            // Load Partitions
            for (int i = 0; i < 9; i++)
                CP[i].Checked = specbools[i, pk-1];
            for (int i = 0; i < 7; i++)
                CL[i].Checked = langbools[i, pk-1];

            if (pk < 650) { CHK_F1.Enabled = true; CHK_F1.Checked = foreignbools[pk - 1]; }
            else { CHK_F1.Enabled = CHK_F1.Checked = false; }

            if (pk > 721)
            {
                for (int i = 0; i < 9; i++)
                    CP[i].Enabled = true;

                for (int i = 0; i < 7; i++)
                    CL[i].Checked = CL[i].Enabled = false;
            }
            else
            {
                CHK_P1.Enabled = true;

                int index = LB_Species.SelectedIndex + 1;
                int gt = PKX.Personal[index].Gender;

                CHK_P2.Enabled = CHK_P4.Enabled = CHK_P6.Enabled = CHK_P8.Enabled = gt != 254; // Not Female-Only
                CHK_P3.Enabled = CHK_P5.Enabled = CHK_P7.Enabled = CHK_P9.Enabled = !(gt == 0 || (gt == 255)); // Not Male-Only and Not Genderless
 
                for (int i = 0; i < 7; i++)
                    CL[i].Enabled = true;
            }
        }
        private void removedropCB(object sender, KeyEventArgs e)
        {
            ((ComboBox)sender).DroppedDown = false;
        }
        private void changeDisplayed(object sender, EventArgs e)
        {
            if (!(sender as CheckBox).Checked)
                return;

            CHK_P6.Checked = sender as CheckBox == CHK_P6;
            CHK_P7.Checked = sender as CheckBox == CHK_P7;
            CHK_P8.Checked = sender as CheckBox == CHK_P8;
            CHK_P9.Checked = sender as CheckBox == CHK_P9;

            CHK_P2.Checked |= CHK_P6.Checked;
            CHK_P3.Checked |= CHK_P7.Checked;
            CHK_P4.Checked |= CHK_P8.Checked;
            CHK_P5.Checked |= CHK_P9.Checked;
        }
        private void changeEncountered(object sender, EventArgs e)
        {
            if (!(CHK_P2.Checked || CHK_P3.Checked || CHK_P4.Checked || CHK_P5.Checked))
                CHK_P6.Checked = CHK_P7.Checked = CHK_P8.Checked = CHK_P9.Checked = false;
            else if (!(CHK_P6.Checked || CHK_P7.Checked || CHK_P8.Checked || CHK_P9.Checked))
            {
                if (sender as CheckBox == CHK_P2 && CHK_P2.Checked)
                    CHK_P6.Checked = true;
                else if (sender as CheckBox == CHK_P3 && CHK_P3.Checked)
                    CHK_P7.Checked = true;
                else if (sender as CheckBox == CHK_P4 && CHK_P4.Checked)
                    CHK_P8.Checked = true;
                else if (sender as CheckBox == CHK_P5 && CHK_P5.Checked)
                    CHK_P9.Checked = true;
            }
        }

        private int species = -1;
        private void setBools()
        {
            if (species < 0) 
                return;

            specbools[0, (species - 1)] = CHK_P1.Checked;
            specbools[1, (species - 1)] = CHK_P2.Checked;
            specbools[2, (species - 1)] = CHK_P3.Checked;
            specbools[3, (species - 1)] = CHK_P4.Checked;
            specbools[4, (species - 1)] = CHK_P5.Checked;
            specbools[5, (species - 1)] = CHK_P6.Checked;
            specbools[6, (species - 1)] = CHK_P7.Checked;
            specbools[7, (species - 1)] = CHK_P8.Checked;
            specbools[8, (species - 1)] = CHK_P9.Checked;
            if (CHK_F1.Enabled) // species < 650 // (1-649)
                foreignbools[species - 1] = CHK_F1.Checked;

            langbools[0, (species - 1)] = CHK_L1.Checked;
            langbools[1, (species - 1)] = CHK_L2.Checked;
            langbools[2, (species - 1)] = CHK_L3.Checked;
            langbools[3, (species - 1)] = CHK_L4.Checked;
            langbools[4, (species - 1)] = CHK_L5.Checked;
            langbools[5, (species - 1)] = CHK_L6.Checked;
            langbools[6, (species - 1)] = CHK_L7.Checked;
        }

        private void B_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void B_Save_Click(object sender, EventArgs e)
        {
            setBools();
            saveChanges();

            // Return back to the parent savefile
            Array.Copy(sav, Main.SAV.Data, sav.Length);
            Main.SAV.Edited = true;
            Close();
        }
        private void saveChanges()
        {
            // Save back the Species Bools 
            // Return to Byte Array        
            for (int p = 0; p < 9; p++)
            {
                byte[] sdata = new byte[0x60];

                for (int i = 0; i < 0x60 * 8; i++)
                    if (specbools[p, i])
                        sdata[i / 8] |= (byte)(1 << i % 8);

                Array.Copy(sdata, 0, sav, Main.SAV.PokeDex + 8 + 0x60 * p, 0x60);
            }

            // Build new bool array for the Languages
            {
                bool[] languagedata = new bool[0x280 * 8];
                for (int i = 0; i < 731; i++)
                    for (int l = 0; l < 7; l++)
                        languagedata[i * 7 + l] = langbools[l, i];

                // Return to Byte Array
                byte[] ldata = new byte[languagedata.Length / 8];

                for (int i = 0; i < languagedata.Length; i++)
                    if (languagedata[i])
                        ldata[i / 8] |= (byte)(1 << i % 8);

                Array.Copy(ldata, 0, sav, Main.SAV.PokeDexLanguageFlags, 0x280);
            }

            // Return Foreign Array
            {
                byte[] foreigndata = new byte[0x52];
                for (int i = 0; i < 0x52 * 8; i++)
                    if (foreignbools[i])
                        foreigndata[i / 8] |= (byte)(1 << i % 8);
                Array.Copy(foreigndata, 0, sav, Main.SAV.PokeDex + 0x64C, 0x52);
            }

            // Store Spinda Spot
            uint PID = Util.getHEXval(TB_Spinda.Text);
            Array.Copy(BitConverter.GetBytes(PID), 0, sav, Main.SAV.Spinda, 4);
        }

        private void getBools()
        {
            // Fill Bit arrays
            for (int i = 0; i < 9; i++)
            {
                byte[] data = new byte[0x60];
                Array.Copy(sav, Main.SAV.PokeDex + 8 + 0x60 * i, data, 0, 0x60);
                BitArray BitRegion = new BitArray(data);
                for (int b = 0; b < 0x60 * 8; b++)
                    specbools[i, b] = BitRegion[b];
            }

            // Fill Language arrays
            byte[] langdata = new byte[0x280];
            Array.Copy(sav, Main.SAV.PokeDexLanguageFlags, langdata, 0, 0x280);
            BitArray LangRegion = new BitArray(langdata);
            for (int b = 0; b < 721; b++) // 721 Species
                for (int i = 0; i < 7; i++) // 7 Languages
                    langbools[i, b] = LangRegion[7 * b + i];

            // Fill Foreign array
            {
                byte[] foreigndata = new byte[0x52];
                Array.Copy(sav, Main.SAV.PokeDex + 0x64C, foreigndata, 0, 0x52);
                BitArray ForeignRegion = new BitArray(foreigndata);
                for (int b = 0; b < 0x52 * 8; b++)
                    foreignbools[b] = ForeignRegion[b];
            }
        }

        private void B_GiveAll_Click(object sender, EventArgs e)
        {
            if (CHK_L1.Enabled)
            {
                CHK_L1.Checked =
                CHK_L2.Checked =
                CHK_L3.Checked =
                CHK_L4.Checked =
                CHK_L5.Checked =
                CHK_L6.Checked =
                CHK_L7.Checked = ModifierKeys != Keys.Control;
            }
            if (CHK_P1.Enabled)
            {
                CHK_P1.Checked = ModifierKeys != Keys.Control;
            }
            if (CHK_F1.Enabled)
            {
                CHK_F1.Checked = ModifierKeys != Keys.Control;
            }
            int index = LB_Species.SelectedIndex+1;
            int gt = PKX.Personal[index].Gender;

            CHK_P2.Checked = CHK_P4.Checked = gt != 254 && ModifierKeys != Keys.Control;
            CHK_P3.Checked = CHK_P5.Checked = gt != 0 && gt != 255 && ModifierKeys != Keys.Control;

            if (ModifierKeys == Keys.Control)
                foreach (var chk in new[] { CHK_P6, CHK_P7, CHK_P8, CHK_P9 })
                    chk.Checked = false;
            else if (!(CHK_P6.Checked || CHK_P7.Checked || CHK_P8.Checked || CHK_P9.Checked))
            {
                if (gt != 254)
                    CHK_P6.Checked = true;
                else
                    CHK_P7.Checked = true;
            }
        }
        private void B_FillDex_Click(object sender, EventArgs e)
        {
            // Write Checkboxes manually (Gender stuff done automatically by form)
            for (int i = 0; i < CB_Species.Items.Count; i++)
            {
                CB_Species.SelectedIndex = i;
                B_GiveAll.PerformClick();
            }

            // Switch to byte editing
            setBools();
            saveChanges();

            // Forms Bool Writing
            for (int i = 0; i < 0x60; i++)
                sav[Main.SAV.PokeDex + 0x368 + i] = 0xFF;

            // Turn off Italian Petlil
            sav[Main.SAV.PokeDexLanguageFlags + 0x1DF] &= 0xFE;

            // Fetch the dex bools
            getBools();
            
            // Reload the current entry
            loadchks();
        }
    }
}
