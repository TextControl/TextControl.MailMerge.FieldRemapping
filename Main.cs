using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TXTextControl.DocumentServer
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void btnOpenDialog_Click(object sender, EventArgs e)
        {
            // create a new DataSet and read the XML file
            DataSet ds = new DataSet();
            ds.ReadXml("sample_db.xml", XmlReadMode.Auto);

            // create a new dialog and use the DataSet and template as parameters
            FieldRemappingDialog frmFieldMapping = 
                new FieldRemappingDialog(
                    "invoice.docx", 
                    TXTextControl.StreamType.WordprocessingML, ds);

            frmFieldMapping.ShowDialog();
        }
    }
}
