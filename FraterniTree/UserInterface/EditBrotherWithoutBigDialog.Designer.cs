namespace FraterniTree.UserInterface
{
    partial class EditBrotherWithoutBigDialog
    {

        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if( disposing && (components != null) )
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblEditBig = new System.Windows.Forms.Label();
            this.tbEditBig = new System.Windows.Forms.TextBox();
            this.btnCancelEdit = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblEditBig
            // 
            this.lblEditBig.AutoSize = true;
            this.lblEditBig.Location = new System.Drawing.Point(9, 26);
            this.lblEditBig.Name = "lblEditBig";
            this.lblEditBig.Size = new System.Drawing.Size(0, 13);
            this.lblEditBig.TabIndex = 42;
            this.lblEditBig.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tbEditBig
            // 
            this.tbEditBig.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.tbEditBig.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbEditBig.Location = new System.Drawing.Point(12, 56);
            this.tbEditBig.Name = "tbEditBig";
            this.tbEditBig.Size = new System.Drawing.Size(242, 20);
            this.tbEditBig.TabIndex = 41;
            this.tbEditBig.TextChanged += new System.EventHandler(this.tbEditBig_TextChanged);
            // 
            // btnCancelEdit
            // 
            this.btnCancelEdit.Enabled = false;
            this.btnCancelEdit.Location = new System.Drawing.Point(156, 93);
            this.btnCancelEdit.Name = "btnCancelEdit";
            this.btnCancelEdit.Size = new System.Drawing.Size(89, 40);
            this.btnCancelEdit.TabIndex = 40;
            this.btnCancelEdit.Text = Util.GetLocalizedString("Cancel");
            this.btnCancelEdit.UseVisualStyleBackColor = true;
            this.btnCancelEdit.Click += new System.EventHandler(this.btnCancelEdit_Click);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(21, 93);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(89, 40);
            this.btnOK.TabIndex = 39;
            this.btnOK.Text = Util.GetLocalizedString("Ok");
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // EditBrotherNoBig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(266, 154);
            this.Controls.Add(this.lblEditBig);
            this.Controls.Add(this.tbEditBig);
            this.Controls.Add(this.btnCancelEdit);
            this.Controls.Add(this.btnOK);
            this.MaximumSize = new System.Drawing.Size(282, 192);
            this.MinimumSize = new System.Drawing.Size(282, 192);
            this.Name = "EditBrotherWithoutBigDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = Util.GetLocalizedString("AddBig");
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblEditBig;
        private System.Windows.Forms.TextBox tbEditBig;
        private System.Windows.Forms.Button btnCancelEdit;
        private System.Windows.Forms.Button btnOK;
    }
}