﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StudentInformationSheet
{
    public partial class StudentSheetPage3 : Form
    {
        public StudentSheetPage3()
        {
            InitializeComponent();
        }

        private void backBut_Click(object sender, EventArgs e)
        {
            this.Hide();
            StudentSheetPage2 studentSheetPage2 = new StudentSheetPage2();
            studentSheetPage2.Show();
        }

        private void nextBtn_Click_1(object sender, EventArgs e)
        {
            this.Hide();
            StudentSheetPage4 studentSheetPage4 = new StudentSheetPage4();
            studentSheetPage4.Show();
        }
    }
}
