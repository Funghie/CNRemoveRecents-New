namespace CNRemoveRecents
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
                if (toolTip1 != null) toolTip1.Dispose();
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
            this.components = new System.ComponentModel.Container();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.nameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pathColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.latestColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.button3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 41);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(491, 24);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.nameColumn,
            this.statusColumn,
            this.pathColumn,
            this.latestColumn});
            this.dataGridView1.Location = new System.Drawing.Point(12, 82);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(858, 544);
            this.dataGridView1.TabIndex = 1;
            // 
            // nameColumn
            // 
            this.nameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.nameColumn.HeaderText = "Project";
            this.nameColumn.MinimumWidth = 6;
            this.nameColumn.Name = "nameColumn";
            this.nameColumn.ReadOnly = true;
            this.nameColumn.Width = 78;
            // 
            // statusColumn
            // 
            this.statusColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.statusColumn.HeaderText = "";
            this.statusColumn.MinimumWidth = 6;
            this.statusColumn.Name = "statusColumn";
            this.statusColumn.ReadOnly = true;
            this.statusColumn.Width = 23;
            // 
            // pathColumn
            // 
            this.pathColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.pathColumn.HeaderText = "Path";
            this.pathColumn.MinimumWidth = 6;
            this.pathColumn.Name = "pathColumn";
            this.pathColumn.ReadOnly = true;
            // 
            // latestColumn
            // 
            this.latestColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.latestColumn.HeaderText = "";
            this.latestColumn.MinimumWidth = 6;
            this.latestColumn.Name = "latestColumn";
            this.latestColumn.ReadOnly = true;
            this.latestColumn.Width = 23;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 632);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(420, 44);
            this.button1.TabIndex = 2;
            this.button1.Text = "Remove Missing";
            this.toolTip1.SetToolTip(this.button1, "Remove Missing (red items)\nThis will remove all missing projects from the recent " +
        "file list");
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(450, 632);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(420, 44);
            this.button2.TabIndex = 3;
            this.button2.Text = "Remove Selected";
            this.toolTip1.SetToolTip(this.button2, "Remove Selected (green items)\nThis will remove all selected projects from the rec" +
        "ent file list");
            this.button2.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(385, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Remove Cubase && Nuendo Recent Files v2 by Phil Pendlebury ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(509, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(132, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "<< Select Application";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(761, 14);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(109, 50);
            this.button3.TabIndex = 6;
            this.button3.Text = "Help";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(882, 688);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.comboBox1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CN Remove Recents v2";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.DataGridViewTextBoxColumn nameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn statusColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn pathColumn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.DataGridViewTextBoxColumn latestColumn;
        private System.Windows.Forms.Button button3;
    }
}

