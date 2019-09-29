using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Model.BuildTimeline
{
    class EventConverter
    {
        private Converter<BuildEventArgs, Event> m_converter;

        public EventConverter()
        {
            m_converter = new Converter<BuildEventArgs, Event>();

            RegisterConverters();
        }

        public Event Convert(BuildEventArgs e)
        {
            return m_converter.Convert(e);
        }

        private void RegisterConverters()
        {
            // Build
            m_converter.Register<BuildStartedEventArgs>(BuildStartedEvent);
            m_converter.Register<BuildFinishedEventArgs>(BuildFinishedEvent);

            // Project
            m_converter.Register<ProjectStartedEventArgs>(ProjectStartedEvent);
            m_converter.Register<ProjectFinishedEventArgs>(ProjectFinishedEvent);

            // Target
            m_converter.Register<TargetStartedEventArgs>(TargetStartedEvent);
            m_converter.Register<TargetFinishedEventArgs>(TargetFinishedEvent);

            // Task
            m_converter.Register<TaskStartedEventArgs>(TaskStartedEvent);
            m_converter.Register<TaskFinishedEventArgs>(TaskFinishedEvent);

            // Message
            m_converter.Register<BuildMessageEventArgs>(BuildMessageEvent);
        }

        // BuildStartedEventArgs => BuildStartedEvent
        private Event BuildStartedEvent(BuildEventArgs buildEvent)
        {
            BuildStartedEventArgs e = buildEvent as BuildStartedEventArgs;
            Debug.Assert(e != null);
            Debug.Assert(e.BuildEventContext == null);

            return new BuildStartedEvent()
            {
                Context = null,
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,
            };
        }

        // BuildFinishedEventArgs => BuildFinishedEvent
        private Event BuildFinishedEvent(BuildEventArgs buildEvent)
        {
            BuildFinishedEventArgs e = buildEvent as BuildFinishedEventArgs;
            Debug.Assert(e != null);
            Debug.Assert(e.BuildEventContext == null);

            return new BuildFinishedEvent()
            {
                Context = null,
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,

                Succeeded = e.Succeeded,
            };
        }

        // ProjectStartedEventArgs => ProjectStartedEvent
        private Event ProjectStartedEvent(BuildEventArgs buildEvent)
        {
            ProjectStartedEventArgs e = buildEvent as ProjectStartedEventArgs;
            Debug.Assert(e != null);

            ProjectEventContext parentContext = null;
            if(e.ParentProjectBuildEventContext != null && e.ParentProjectBuildEventContext != BuildEventContext.Invalid)
            {
                Debug.Assert(e.ParentProjectBuildEventContext.TargetId == BuildEventContext.InvalidTargetId);
                Debug.Assert(e.ParentProjectBuildEventContext.TaskId == BuildEventContext.InvalidTaskId);

                parentContext = new ProjectEventContext()
                {
                    NodeId = e.ParentProjectBuildEventContext.NodeId,
                    ContextId = e.ParentProjectBuildEventContext.ProjectContextId,
                    ProjectId = e.ParentProjectBuildEventContext.ProjectInstanceId,
                };
            }

            return new ProjectStartedEvent()
            {
                Context = new ProjectEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = e.BuildEventContext.ProjectInstanceId
                },
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,

                ParentProjectEventContext = parentContext,
                ProjectFile = e.ProjectFile,
                ProjectId = e.ProjectId,
            };
        }

        // ProjectFinishedEventArgs => ProjectFinishedEvent
        private Event ProjectFinishedEvent(BuildEventArgs buildEvent)
        {
            ProjectFinishedEventArgs e = buildEvent as ProjectFinishedEventArgs;
            Debug.Assert(e != null);

            return new ProjectFinishedEvent()
            {
                Context = new ProjectEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = e.BuildEventContext.ProjectInstanceId
                },
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,

                ProjectFile = e.ProjectFile,
                Succeeded = e.Succeeded,
            };
        }

        // TargetStartedEventArgs => TargetStartedEvent
        private Event TargetStartedEvent(BuildEventArgs buildEvent)
        {
            TargetStartedEventArgs e = buildEvent as TargetStartedEventArgs;
            Debug.Assert(e != null);

            return new TargetStartedEvent()
            {
                Context = new TargetEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = e.BuildEventContext.ProjectInstanceId,
                    TargetId = e.BuildEventContext.TargetId,
                },
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,

                ProjectFile = e.ProjectFile,
                ParentTarget = e.ParentTarget,
                TargetFile = e.TargetFile,
                TargetName = e.TargetName,
            };
        }

        /// TargetFinishedEventArgs => TargetFinishedEvent
        private Event TargetFinishedEvent(BuildEventArgs buildEvent)
        {
            TargetFinishedEventArgs e = buildEvent as TargetFinishedEventArgs;
            Debug.Assert(e != null);
            
            return new TargetFinishedEvent()
            {
                Context = new TargetEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = e.BuildEventContext.ProjectInstanceId,
                    TargetId = e.BuildEventContext.TargetId,
                },
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,

                ProjectFile = e.ProjectFile,
                Succeeded = e.Succeeded,
                TargetFile = e.TargetFile,
                TargetName = e.TargetName,
            };
        }

        // TaskStartedEventArgs => TaskStartedEvent
        private Event TaskStartedEvent(BuildEventArgs buildEvent)
        {
            TaskStartedEventArgs e = buildEvent as TaskStartedEventArgs;
            Debug.Assert(e != null);

            return new TaskStartedEvent()
            {
                Context = new TaskEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = e.BuildEventContext.ProjectInstanceId,
                    TargetId = e.BuildEventContext.TargetId,
                    TaskId = e.BuildEventContext.TaskId,
                },
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,

                ProjectFile = e.ProjectFile,
                TaskFile = e.TaskFile,
                TaskName = e.TaskName,
            };
        }

        // TaskFinishedEventArgs => TaskFinishedEvent
        private Event TaskFinishedEvent(BuildEventArgs buildEvent)
        {
            TaskFinishedEventArgs e = buildEvent as TaskFinishedEventArgs;
            Debug.Assert(e != null);

            return new TaskFinishedEvent()
            {
                Context = new TaskEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = e.BuildEventContext.ProjectInstanceId,
                    TargetId = e.BuildEventContext.TargetId,
                    TaskId = e.BuildEventContext.TaskId,
                },
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,

                ProjectFile = e.ProjectFile,
                Succeeded = e.Succeeded,
                TaskFile = e.TaskFile,
                TaskName = e.TaskName,
            };
        }

        // BuildMessageEventArgs => MessageEvent
        private Event BuildMessageEvent(BuildEventArgs buildEvent)
        {
            BuildMessageEventArgs e = buildEvent as BuildMessageEventArgs;
            Debug.Assert(e != null);

            return new MessageEvent()
            {
                Context = new MessageEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = e.BuildEventContext.ProjectInstanceId,
                    TargetId = e.BuildEventContext.TargetId,
                    TaskId = e.BuildEventContext.TaskId,
                },
                Message = e.Message,
                ThreadId = e.ThreadId,
                Timestamp = e.Timestamp,
                
                Code = e.Code,
                ProjectFile = e.ProjectFile,
                File = e.File,
                Subcategory = e.Subcategory,
                LineStart = e.LineNumber,
                LineEnd = e.EndLineNumber,
                ColumnStart = e.ColumnNumber,
                ColumnEnd = e.EndColumnNumber,
            };
        }
    }
}
