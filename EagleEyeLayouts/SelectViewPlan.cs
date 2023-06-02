using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EagleEyeLayouts
{
	public partial class SelectViewPlan : Form
	{
		public static string SelectedViewPlanName = string.Empty;
		
		public SelectViewPlan(List<string> viewPlanNames)
		{
			InitializeComponent();

			foreach (string viewPlanName in viewPlanNames)
			{
				viewPlansList.Items.Add(viewPlanName);
			}
			viewPlansList.SelectedIndex = 0;
			
		}

		private void selectViewBtn_Click(object sender, EventArgs e)
		{
			SelectedViewPlanName = viewPlansList.SelectedItem.ToString();
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
