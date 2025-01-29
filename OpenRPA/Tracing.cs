﻿using Newtonsoft.Json.Linq;
using OpenRPA.Views;
using System;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;

namespace OpenRPA.Interfaces
{
    public class Tracing : TraceListener, System.ComponentModel.INotifyPropertyChanged //, ILogEventSink
    {
        // private int maxLines = 20;
        private string _TraceMessages = "";
        public string TraceMessages
        {
            get
            {
                string result = string.Empty;
                result = _TraceMessages;
                var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int count = lines.Count();
                if (count > Config.local.max_trace_lines)
                {
                    var list = lines.ToList();
                    list.RemoveRange((Config.local.max_trace_lines - 10), (count - Config.local.max_trace_lines) + 10);
                    _TraceMessages = string.Join(Environment.NewLine, list);
                }
                return _TraceMessages;
            }
            set
            {
                _TraceMessages = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Trace"));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("TraceMessages"));
            }
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            base.TraceEvent(eventCache, source, eventType, id);
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            base.TraceEvent(eventCache, source, eventType, id, format, args);
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            base.TraceEvent(eventCache, source, eventType, id, message);
        }
        public IWorkflowInstance workflowInstance { get; set; }
        private string _OutputMessages = "";
        public string OutputMessages
        {
            get
            {
                string result = "";
                result = _OutputMessages;
                var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int count = lines.Count();
                if (count > Config.local.max_output_lines)
                {
                    var list = lines.ToList();
                    list.RemoveRange((Config.local.max_output_lines - 10), (count - Config.local.max_output_lines) + 10);
                    _OutputMessages = string.Join(Environment.NewLine, list);
                }
                return _OutputMessages;
            }
            set
            {
                _OutputMessages = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
            }
        }
        public override void Write(object o)
        {
            if (o != null) Write(o.ToString());
        }
        public override void Write(object o, string category)
        {
            if (o != null) Write(o.ToString(), category);
        }
        public override void WriteLine(object o)
        {
            if (o != null) WriteLine(o.ToString());
        }
        public override void WriteLine(object o, string category)
        {
            if (o != null) WriteLine(o.ToString(), category);
        }
        public override void Write(string message)
        {
            Write(message, "Output");
        }
        public override void WriteLine(string message)
        {
            WriteLine(message, "Output");
        }
        public override void Write(string message, string category)
        {
            WriteLine(message);
        }
        private static string logpath = "";
        public static ThreadLocal<string> InstanceId = new ThreadLocal<string>();
        public override void WriteLine(string message, string category)
        {
            try
            {
                if (string.IsNullOrEmpty(logpath))
                {
                    logpath = Extensions.ProjectsDirectory;
                }
                try
                {
                    IProject project = null;
                    IWorkflow workflow = null;
                    if (InstanceId.IsValueCreated)
                    {
                        var i = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId.Value).LastOrDefault();
                        // message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                        workflow = i?.Workflow;
                        project = i?.Workflow?.Project();
                    }
                    
                    // if (i.console == null) i.console = new List<WorkflowConsoleLog>();
                    int lvl = 7;
                    if (category == "Error")
                    {
                        lvl = 0;
                        if(project != null) RobotInstance.LocalLogProvider?.LogError("[{projectname}][{workflowname}][{workflowname}] " + message, project.name, workflow.name);
                        if (project == null) RobotInstance.LocalLogProvider?.LogError(message);
                    }
                    else if (category == "Warning")
                    {
                        lvl = 1;
                        if (project != null) RobotInstance.LocalLogProvider?.LogWarning("[{projectname}][{workflowname}] " + message, project.name, workflow.name);
                        if (project == null) RobotInstance.LocalLogProvider?.LogWarning(message);
                    }
                    else if (category == "Output" || category == "Information" || category == "")
                    {
                        lvl = 2;
                        if (project != null) RobotInstance.LocalLogProvider?.LogInformation("[{projectname}][{workflowname}] " + message, project.name, workflow.name);
                        if (project == null) RobotInstance.LocalLogProvider?.LogInformation(message);
                    }
                    else if (category == "Debug")
                    {
                        lvl = 3;
                        if (project != null) RobotInstance.LocalLogProvider?.LogDebug("[{projectname}][{workflowname}] " + message, project.name, workflow.name);
                        if (project == null) RobotInstance.LocalLogProvider?.LogDebug(message);
                    }
                    else if (category == "Verbose")
                    {
                        lvl = 4;
                        if (project != null) RobotInstance.LocalLogProvider?.LogTrace("[{projectname}][{workflowname}] " + message, project.name, workflow.name);
                        if (project == null) RobotInstance.LocalLogProvider?.LogTrace(message);
                    }
                    else if (category == "network")
                    {
                        if (project != null) RobotInstance.LocalLogProvider?.LogTrace("[{projectname}][{workflowname}] " + message, project.name, workflow.name);
                        if (project == null) RobotInstance.LocalLogProvider?.LogTrace(message);
                    }
                    else
                    {
                        if (project != null) RobotInstance.LocalLogProvider?.LogTrace("[{projectname}][{workflowname}] " + message, project.name, workflow.name);
                        if (project == null) RobotInstance.LocalLogProvider?.LogTrace(message);
                    }

                    if (InstanceId.IsValueCreated)
                    {
                        var i = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId.Value).LastOrDefault();
                        // message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                        if (i != null && i.Workflow != null && project != null && category != "Network")
                        {
                            if ((i.Workflow.save_output || project.save_output) && !Config.local.skip_online_state)
                            {
                                var msg = new WorkflowConsoleLog() { msg = message, lvl = lvl };
                                if (Monitor.TryEnter(i, 1000))
                                {
                                    try
                                    {
                                        if (i.console == null) i.console = new List<WorkflowConsoleLog>();
                                        i.console.Insert(0, msg);
                                        i.isDirty = true;
                                    }
                                    finally
                                    {
                                        Monitor.Exit(i);
                                    }
                                }
                            }
                            if ((i.Workflow.send_output || project.send_output) && !string.IsNullOrEmpty(i.queuename) && !string.IsNullOrEmpty(i.correlationId))
                            {
                                try
                                {
                                    mq.RobotOutputCommand command = new mq.RobotOutputCommand();
                                    command.command = "output";
                                    command.level = lvl;
                                    command.workflowid = i.WorkflowId;
                                    command.data = message;
                                    global.webSocketClient.QueueMessage(i.queuename, command, null, i.correlationId, 0, true, i.TraceId, i.SpanId);
                                }
                                catch (Exception)
                                {
                                }
                            }
                            //message = "[" + i.queuename + "]" + message;
                            //message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                        }
                        else
                        {
                            //message = "[" + Thread.CurrentThread.ManagedThreadId + "][" + InstanceId.Value + "]" + message;
                        }
                    }
                    else
                    {
                        //message = "[" + Thread.CurrentThread.ManagedThreadId + "][null]" + message;
                    }
                }
                catch (Exception)
                {
                }

                if (category == "Tracing") return;
                DateTime dt = DateTime.Now;
                if (category == "Output")
                {
                    _OutputMessages = _OutputMessages.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
                    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("OutputMessages"));
                }
                if (category == "Output" && !Config.local.log_output) return;
                _TraceMessages = _TraceMessages.Insert(0, string.Format(@"[{0:HH\:mm\:ss\.fff}][{1}] {2}" + Environment.NewLine, dt, category, message));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Trace"));
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("TraceMessages"));
            }
            catch (Exception)
            {
            }
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public DateTime lastEvent = DateTime.Now;
        protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                Task.Run(() => { PropertyChanged?.Invoke(this, e); });
            }
        }
        public IFormatProvider formatProvider { get; set; }
    }
}
