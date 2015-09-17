/*------------------------------------------------------------------------
** module:      TXTextControl.DocumentServer.FieldRemappingDialog
** file:        FieldRemappingDialog.cs
 *
** description:	This file contains the prototype implementation of a
**              remapping dialog
**----------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TXTextControl;
using TXTextControl.DocumentServer.Fields;

namespace TXTextControl.DocumentServer
{
    /*--------------------------------------------------------------------
    ** FieldRemappingDialog class
    **------------------------------------------------------------------*/
    public partial class FieldRemappingDialog : Form
    {
        /*----------------------------------------------------------------
        ** Private fields and member variables
        **--------------------------------------------------------------*/
        private string m_template;
        private DataSet m_datasource;
        private StreamType templateStreamType;
        private List<ApplicationField> listApplicationFields;
        private bool bLoaded;
        private int currentApplicationField = -1;
        private bool bDirtyFlag = false;

        /*----------------------------------------------------------------
        ** Public properties
        **--------------------------------------------------------------*/
        public string Template
        {
            get { return m_template; }
        }

        public DataSet DataSource
        {
            get { return m_datasource; }
        }

        /*----------------------------------------------------------------
        ** Constructor
         * 
         * Parameters:  templatePath - the template to be loaded
         *              streamType - the document format of the template
         *              dataSource - new fields for the remapping
        **--------------------------------------------------------------*/
        public FieldRemappingDialog(string templatePath, StreamType streamType, DataSet dataSource)
        {
            InitializeComponent();

            m_template = templatePath;
            m_datasource = dataSource;
            templateStreamType = streamType;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listView1.Items.Clear();

            foreach (DataColumn dc in ((DataTable)comboBox1.SelectedItem).Columns)
            {
                listView1.Items.Add(dc.ColumnName, 0);
            }
        }

        /*----------------------------------------------------------------
        ** Form Load event
        **--------------------------------------------------------------*/
        private void frmFieldMapping_Load(object sender, EventArgs e)
        {
            bLoaded = true;

            // fill the combo box with available tables
            foreach (DataTable dt in m_datasource.Tables)
            {
                comboBox1.Items.Add(dt);
            }

            comboBox1.DisplayMember = "TableName";

            if (comboBox1.Items.Count != 0)
                comboBox1.SelectedIndex = 1;

            // load the template into TX Text Control
            TXTextControl.LoadSettings ls = new LoadSettings();
            ls.ApplicationFieldFormat = ApplicationFieldFormat.MSWord;

            textControl1.Load(m_template, templateStreamType, ls);
            
            FitToWindow();
            FillApplicationFieldList();
            EnableNavigateButtons();
        }

        /*----------------------------------------------------------------
        ** FillApplicationFieldList method
         *
         * description: Loads all main merge fields into a private list
        **--------------------------------------------------------------*/
        private void FillApplicationFieldList()
        {
            listApplicationFields = new List<ApplicationField>();

            foreach (ApplicationField field in textControl1.ApplicationFields)
            {
                if (field.TypeName == "MERGEFIELD")
                    listApplicationFields.Add(field);
            }
        }

        /*----------------------------------------------------------------
        ** HighlightField method
         *
         * description: Highlights the current merge field
        **--------------------------------------------------------------*/
        private void HighlightField()
        {
            if (listApplicationFields.Count <= currentApplicationField ||
                currentApplicationField == -1)
                return;

            // get the current field from the list
            ApplicationField currentField = 
                listApplicationFields[currentApplicationField];

            // scroll to the field location
            textControl1.ScrollLocation = new Point(currentField.Bounds.X, currentField.Bounds.Y);
            textControl1.Refresh();

            Graphics g = textControl1.CreateGraphics();

            Pen myPen = new Pen(Color.Red, 2);
            SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(128, 255, 0, 0));

            // draw a transparent rectangle at the field location
            g.FillRectangle(semiTransBrush, ((currentField.Bounds.X - textControl1.ScrollLocation.X) / DpiX) * ((float)textControl1.ZoomFactor / 100),
                ((currentField.Bounds.Y - textControl1.ScrollLocation.Y) / DpiX) * ((float)textControl1.ZoomFactor / 100),
                (currentField.Bounds.Width / DpiX) * ((float)textControl1.ZoomFactor / 100),
                (currentField.Bounds.Height / DpiX) * ((float)textControl1.ZoomFactor / 100));

            textControl1.Selection.Length = 0;
        }

        // returns the resolution factor to convert twips to pixels
        private int DpiX { get { Graphics g = Graphics.FromHwnd(IntPtr.Zero); return (int)(1440 / g.DpiX); } }

        /*----------------------------------------------------------------
        ** FitToWindow method
         *
         * description: Fits the visible page to the control widths
        **--------------------------------------------------------------*/
        private void FitToWindow()
        {
            textControl1.PageUnit = MeasuringUnit.Twips;
            int iVisibleGap = 65;

            SectionFormat currentSection = textControl1.Sections.GetItem().Format;

            double widthZoom = 100 * textControl1.Width /
                ((currentSection.PageSize.Width / DpiX)
                + iVisibleGap);
            double heightZoom = 100 * textControl1.Height /
                ((currentSection.PageSize.Height / DpiX)
                + iVisibleGap);

            if (widthZoom < heightZoom)
                textControl1.ZoomFactor = (int)widthZoom;
            else
                textControl1.ZoomFactor = (int)heightZoom;
        }

        #region "Resize and zoom events"

        private void textControl1_Resize(object sender, EventArgs e)
        {
            if (bLoaded == true)
            {
                FitToWindow();

                if (currentApplicationField != -1)
                    HighlightField();
            }
        }

        private void textControl1_Zoomed(object sender, EventArgs e)
        {
            if (currentApplicationField != -1)
                HighlightField();
        }

        #endregion

        #region "Navigation buttons"

        // increases the current field
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentApplicationField != listApplicationFields.Count - 1)
            {
                currentApplicationField++;
                HighlightField();
                EnableNavigateButtons();
            }
        }

        // decreases the current field
        private void btnLast_Click(object sender, EventArgs e)
        {
            if (currentApplicationField != -1)
            {
                currentApplicationField--;
                HighlightField();
                EnableNavigateButtons();
            }
        }

        #endregion

        /*----------------------------------------------------------------
        ** EnableNavigateButtons method
         *
         * description: Based on the currrent state, navigation buttons
         *              are enabled or disabled
        **--------------------------------------------------------------*/
        private void EnableNavigateButtons()
        {
            btnNext.Enabled = true;
            btnBack.Enabled = true;
          
            btnDelete.Enabled = currentApplicationField == -1 ? false : true;
            listView1.Enabled = currentApplicationField == -1 ? false : true;

            if (currentApplicationField == -1 || currentApplicationField == 0)
            {
                btnBack.Enabled = false;
            }

            if (currentApplicationField == listApplicationFields.Count - 1)
            {
                btnNext.Enabled = false;
            }

            if(currentApplicationField != -1)
                textBox1.Text = listApplicationFields[currentApplicationField].Parameters[0];
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            bDirtyFlag = true;

            // get the current field and change the name and text based on the selection
            MergeField field = new MergeField(listApplicationFields[currentApplicationField]);
            field.Name = listView1.SelectedItems[0].Text;
            field.Text = listView1.SelectedItems[0].Text;

            EnableNavigateButtons();
            HighlightField();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            bDirtyFlag = true;

            // remove the currently selected field
            textControl1.ApplicationFields.Remove(listApplicationFields[currentApplicationField]);

            // reset the list and the current field
            currentApplicationField = -1;
            EnableNavigateButtons();
            FillApplicationFieldList();
            HighlightField();
        }

        private void textControl1_TextFieldEntered(object sender, TextFieldEventArgs e)
        {
            // set the current field based on the selected field
            currentApplicationField = listApplicationFields.IndexOf(e.TextField as ApplicationField);
            HighlightField();
            EnableNavigateButtons();
        }

        private void FieldRemappingDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bDirtyFlag == false)
                return;

            // save the document on closing or cancel action
            System.Windows.Forms.DialogResult dlgRslt = MessageBox.Show("Do you want to save the changes?", "Save template?", MessageBoxButtons.YesNoCancel);

            if (dlgRslt == System.Windows.Forms.DialogResult.Yes)
            {
                textControl1.Save(m_template, templateStreamType);
            }
            else if (dlgRslt == System.Windows.Forms.DialogResult.Cancel)
                e.Cancel = true;
        }

        private void textControl1_Changed(object sender, EventArgs e)
        {
            bDirtyFlag = true;
        }
                
    }
}

