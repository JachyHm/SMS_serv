namespace SMS_srv_editor
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.wdir_combobox = new System.Windows.Forms.ComboBox();
            this.select_wdir_lab = new System.Windows.Forms.Label();
            this.select_wdir_but = new System.Windows.Forms.Button();
            this.verbose_checkbox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.update_interval_updown = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.select_xmlpat_button = new System.Windows.Forms.Button();
            this.xmlpat_combobox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.select_numpat_button = new System.Windows.Forms.Button();
            this.numpat_combobox = new System.Windows.Forms.ComboBox();
            this.phonenumber_table = new System.Windows.Forms.DataGridView();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.number = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.update_interval_updown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.phonenumber_table)).BeginInit();
            this.SuspendLayout();
            // 
            // wdir_combobox
            // 
            this.wdir_combobox.AllowDrop = true;
            this.wdir_combobox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.wdir_combobox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.wdir_combobox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.wdir_combobox.Location = new System.Drawing.Point(12, 29);
            this.wdir_combobox.Name = "wdir_combobox";
            this.wdir_combobox.Size = new System.Drawing.Size(534, 21);
            this.wdir_combobox.TabIndex = 0;
            this.wdir_combobox.SelectedIndexChanged += new System.EventHandler(this.wdir_combobox_SelectedIndexChanged);
            this.wdir_combobox.Enter += new System.EventHandler(this.wdir_combobox_Enter);
            this.wdir_combobox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.wdir_combobox_KeyUp);
            this.wdir_combobox.Leave += new System.EventHandler(this.wdir_combobox_Leave);
            // 
            // select_wdir_lab
            // 
            this.select_wdir_lab.AutoSize = true;
            this.select_wdir_lab.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.select_wdir_lab.Location = new System.Drawing.Point(8, 2);
            this.select_wdir_lab.Name = "select_wdir_lab";
            this.select_wdir_lab.Size = new System.Drawing.Size(215, 24);
            this.select_wdir_lab.TabIndex = 1;
            this.select_wdir_lab.Text = "Select working directory:";
            // 
            // select_wdir_but
            // 
            this.select_wdir_but.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.select_wdir_but.Location = new System.Drawing.Point(552, 27);
            this.select_wdir_but.Name = "select_wdir_but";
            this.select_wdir_but.Size = new System.Drawing.Size(75, 23);
            this.select_wdir_but.TabIndex = 2;
            this.select_wdir_but.Text = "Browse";
            this.select_wdir_but.UseVisualStyleBackColor = true;
            this.select_wdir_but.Click += new System.EventHandler(this.select_wdir_but_Click);
            // 
            // verbose_checkbox
            // 
            this.verbose_checkbox.AutoSize = true;
            this.verbose_checkbox.Location = new System.Drawing.Point(16, 80);
            this.verbose_checkbox.Name = "verbose_checkbox";
            this.verbose_checkbox.Size = new System.Drawing.Size(128, 17);
            this.verbose_checkbox.TabIndex = 4;
            this.verbose_checkbox.Text = "Display log in console";
            this.verbose_checkbox.UseVisualStyleBackColor = true;
            this.verbose_checkbox.CheckedChanged += new System.EventHandler(this.verbose_checkbox_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(8, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 24);
            this.label1.TabIndex = 5;
            this.label1.Text = "Configuration:";
            // 
            // update_interval_updown
            // 
            this.update_interval_updown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.update_interval_updown.Location = new System.Drawing.Point(500, 79);
            this.update_interval_updown.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.update_interval_updown.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.update_interval_updown.Name = "update_interval_updown";
            this.update_interval_updown.Size = new System.Drawing.Size(52, 20);
            this.update_interval_updown.TabIndex = 6;
            this.update_interval_updown.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.update_interval_updown.ValueChanged += new System.EventHandler(this.update_interval_updown_ValueChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(412, 81);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Update interval:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(558, 81);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "ms";
            // 
            // select_xmlpat_button
            // 
            this.select_xmlpat_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.select_xmlpat_button.Location = new System.Drawing.Point(552, 120);
            this.select_xmlpat_button.Name = "select_xmlpat_button";
            this.select_xmlpat_button.Size = new System.Drawing.Size(75, 23);
            this.select_xmlpat_button.TabIndex = 10;
            this.select_xmlpat_button.Text = "Browse";
            this.select_xmlpat_button.UseVisualStyleBackColor = true;
            this.select_xmlpat_button.Click += new System.EventHandler(this.select_xmlpat_button_Click);
            // 
            // xmlpat_combobox
            // 
            this.xmlpat_combobox.AllowDrop = true;
            this.xmlpat_combobox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.xmlpat_combobox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.xmlpat_combobox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.xmlpat_combobox.Location = new System.Drawing.Point(12, 122);
            this.xmlpat_combobox.Name = "xmlpat_combobox";
            this.xmlpat_combobox.Size = new System.Drawing.Size(534, 21);
            this.xmlpat_combobox.TabIndex = 9;
            this.xmlpat_combobox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.xmlpat_combobox_KeyDown);
            this.xmlpat_combobox.Leave += new System.EventHandler(this.xmlpat_combobox_Leave);
            this.xmlpat_combobox.Validated += new System.EventHandler(this.xmlpat_combobox_Validated);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 106);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "XML file:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 146);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(72, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Phone list file:";
            // 
            // select_numpat_button
            // 
            this.select_numpat_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.select_numpat_button.Location = new System.Drawing.Point(552, 160);
            this.select_numpat_button.Name = "select_numpat_button";
            this.select_numpat_button.Size = new System.Drawing.Size(75, 23);
            this.select_numpat_button.TabIndex = 14;
            this.select_numpat_button.Text = "Browse";
            this.select_numpat_button.UseVisualStyleBackColor = true;
            this.select_numpat_button.Click += new System.EventHandler(this.select_numpat_button_Click);
            // 
            // numpat_combobox
            // 
            this.numpat_combobox.AllowDrop = true;
            this.numpat_combobox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numpat_combobox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.numpat_combobox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.numpat_combobox.Enabled = false;
            this.numpat_combobox.Location = new System.Drawing.Point(12, 162);
            this.numpat_combobox.Name = "numpat_combobox";
            this.numpat_combobox.Size = new System.Drawing.Size(534, 21);
            this.numpat_combobox.TabIndex = 13;
            // 
            // phonenumber_table
            // 
            this.phonenumber_table.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.phonenumber_table.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.phonenumber_table.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.phonenumber_table.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.phonenumber_table.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.name,
            this.number});
            this.phonenumber_table.Location = new System.Drawing.Point(12, 189);
            this.phonenumber_table.Name = "phonenumber_table";
            this.phonenumber_table.Size = new System.Drawing.Size(616, 280);
            this.phonenumber_table.TabIndex = 15;
            this.phonenumber_table.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.phonenumber_table_CellEndEdit);
            // 
            // name
            // 
            this.name.HeaderText = "Name";
            this.name.Name = "name";
            // 
            // number
            // 
            this.number.HeaderText = "Phone number";
            this.number.Name = "number";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 481);
            this.Controls.Add(this.phonenumber_table);
            this.Controls.Add(this.select_numpat_button);
            this.Controls.Add(this.numpat_combobox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.select_xmlpat_button);
            this.Controls.Add(this.xmlpat_combobox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.update_interval_updown);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.verbose_checkbox);
            this.Controls.Add(this.select_wdir_but);
            this.Controls.Add(this.select_wdir_lab);
            this.Controls.Add(this.wdir_combobox);
            this.MinimumSize = new System.Drawing.Size(385, 282);
            this.Name = "Form1";
            this.Text = "Grafický editor SMS serveru";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.update_interval_updown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.phonenumber_table)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ComboBox wdir_combobox;
        private System.Windows.Forms.Label select_wdir_lab;
        private System.Windows.Forms.Button select_wdir_but;
        private System.Windows.Forms.CheckBox verbose_checkbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown update_interval_updown;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button select_xmlpat_button;
        private System.Windows.Forms.ComboBox xmlpat_combobox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button select_numpat_button;
        private System.Windows.Forms.ComboBox numpat_combobox;
        private System.Windows.Forms.DataGridView phonenumber_table;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn number;
    }
}

