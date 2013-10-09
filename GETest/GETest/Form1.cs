using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
            Skybound.Gecko.Xpcom.Initialize(@"C:\Users\Daniel\tools\xulrunner-1.9.1.2.en-US.win32\xulrunner\");
            //Skybound.Gecko.Xpcom.Initialize(@"C:\Users\Jake\Dropbox\UF\Semesters\13 Fall\NUI\Project\GeckoFX\xulrunner-1.9.1.2.en-US.win32\xulrunner\");
            geckoWebBrowser1.BackColor = Color.White;
            geckoWebBrowser1.NoDefaultContextMenu = true;
            geckoWebBrowser1.HandleCreated += new EventHandler(geckoBrowser1_HandleCreated);
           // geckoWebBrowser1.KeyDown+=new EventHandler(geckoWebBrowser1_KeyDown);
        }


        //Handle Created EventHandler
        private void geckoBrowser1_HandleCreated(object sender, EventArgs e)
        {
            geckoWebBrowser1.Navigate(@"http://earth-api-samples.googlecode.com/svn/trunk/demos/desktop-embedded/pluginhost.html");
        }

        private void tb_Gestures_Enter(object sender, EventArgs e)
        {
            System.Windows.Forms.SendKeys.SendWait("TOUCHED");
            Debug.WriteLine("ENTER");
        }

        private void geckoWebBrowser1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.SendKeys.Send("W");
            Debug.WriteLine("Clicked");
        }

        private void geckoWebBrowser1_MouseUp(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("M_UP");
        }

        private void geckoWebBrowser1_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine("KEY PRESSED");
        }

     


    }
}
