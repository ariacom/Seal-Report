//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Windows.Forms;
using Seal.AI;
using Seal.Model;

namespace Seal.Forms
{
    public partial class AIConfigurationEditorForm : Form
    {
        AIServerConfiguration _configuration;

        public AIConfigurationEditorForm(AIServerConfiguration configuration)
        {
            Visible = false;
            _configuration = configuration;

            InitializeComponent();
            toolStripStatusLabel.Image = null;

            configuration.InitEditor();
            mainPropertyGrid.ToolbarVisible = false;
            mainPropertyGrid.PropertySort = PropertySort.Categorized;
            mainPropertyGrid.LineColor = System.Drawing.SystemColors.ControlLight;
            mainPropertyGrid.SelectedObject = _configuration;

            Text = Repository.SealRootProductName + " AI Configuration Editor";

            ShowIcon = true;
            Icon = Properties.Resources.serverManager;
        }

        private void AIConfigurationEditorForm_Load(object sender, EventArgs e)
        {
            infoTextBox.Text = string.Join(Environment.NewLine, new[]
            {
                @"Configure the AI Providers, Tools and Agents used by the Report Server.",
                "",
                @"AI Providers define the connection to an AI service (OpenAI, Anthropic, Azure, Ollama...).",
                @"AI Tools define functions that can be called by the AI model during a conversation.",
                @"AI Agents combine a provider, an optional set of tools, and a default system prompt.",
                "",
                @"Changes are saved to Settings\AI\AIConfiguration.xml in the repository.",
                @"New values may require a restart of the Report Designer or the Web Server."
            });

            Visible = true;
            ActiveControl = mainStatusStrip;
        }

        private void cancelToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void okToolStripButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
