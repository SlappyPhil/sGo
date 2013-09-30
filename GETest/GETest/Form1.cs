using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GETest
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Skybound.Gecko.Xpcom.Initialize(@"D:\xulrunner-1.9.1.1.en-US.win32\xulrunner\");
            geckoWebBrowser1.BackColor = Color.White;
            geckoWebBrowser1.NoDefaultContextMenu = true;
            geckoWebBrowser1.HandleCreated += new EventHandler(geckoBrowser1_HandleCreated);
         
        }

        //Handle Created EventHandler
        private void geckoBrowser1_HandleCreated(object sender, EventArgs e)
        {
            geckoWebBrowser1.Navigate(@"http://earth-api-samples.googlecode.com/svn/trunk/demos/desktop-embedded/pluginhost.html");
        }
    }
}
