﻿using MetroFramework.Forms;
using SqlAuditor.Config;
using SqlAuditor.Trace;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SqlAuditor.Win
{
    public partial class frmTest : MetroForm, ITraceObserver
    {
        TraceConfig trace;
        TraceContext context;
        SqlInstanceAuditor auditor;
        List<int> columns = new List<int>();
        bool running = false;
        public frmTest(TraceConfig trace)
        {
            InitializeComponent();
            this.trace = trace;
            this.Text += trace.Instance.DataSource;
        }

        private void frmTest_Load(object sender, EventArgs e)
        {

            context = new TraceContext(trace, (new ITraceObserver[]{this}).ToList());
            trace.Events.ForEach((evt) => evt.Columns.ToList().ForEach((c) =>
            {
                if (!columns.Contains(c)) columns.Add(c);
            }));
            columns.ForEach((c) =>
            {
                var col = new DataGridViewTextBoxColumn();
                col.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
                col.Resizable = DataGridViewTriState.True;
                col.HeaderText = context.SqlTraceColumns[c].Name;
                col.MinimumWidth = 60;
                col.Name = "colT" + c.ToString();
                col.ReadOnly = true;
                grdEvents.Columns.Add(col);
            });
            

        }

        void ITraceObserver.Init(TraceContext context)
        {

        }

        void ITraceObserver.EventReceived(TraceEvent traceEvent)
        {
            var evt = context.SqlTraceEvents[traceEvent.EventClass];
            var evtCtgr = context.SqlTraceCategories[evt.CategoryId];
            this.Invoke(new Action(()=>{
                var rowIdx = grdEvents.Rows.Add();
                var row = grdEvents.Rows[rowIdx];
                row.Cells["colCategory"].Value = evtCtgr.Name;
                row.Cells["colEvent"].Value = evt.Name;
                foreach (var kv in traceEvent.Values)
                {
                    if(columns.Contains(kv.Key))
                    row.Cells["colT" + kv.Key].Value = kv.Value;
                }
            }));
            
        }

        private void frmTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(running) auditor.Stop();
        }

        private void frmTest_Shown(object sender, EventArgs e)
        {
            
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (running)
            {
                auditor.Stop();
                auditor.Dispose();
                auditor = null;
                cbUseEmailProvider.Enabled = true;
                btnStartStop.Text = "Start";
                running = false;
            }
            else
            {
                if(cbUseEmailProvider.Checked)
                {
                    if (!context.TraceObservers.Where((ob) => ob is EmailProvider).Any())
                    {
                        var emailProvider = new EmailProvider();
                        emailProvider.Init(context);
                        context.TraceObservers.Add(emailProvider);
                    }
                }
                else {
                    if (context.TraceObservers.Where((ob) => ob is EmailProvider).Any())
                    {
                        context.TraceObservers.Remove(context.TraceObservers.Find((ob)=>ob is EmailProvider));
                    }
                }
                auditor = new SqlInstanceAuditor(context);
                auditor.Start();
                cbUseEmailProvider.Enabled = false;
                btnStartStop.Text = "Stop";
                running = true;
            }
        }
    }
}
