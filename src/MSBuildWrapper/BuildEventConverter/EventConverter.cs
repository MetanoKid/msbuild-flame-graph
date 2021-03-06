﻿using BuildTimeline;
using Microsoft.Build.Framework;
using Model;
using System.Diagnostics;

namespace MSBuildWrapper
{
    public class EventConverter
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

            // Messages
            m_converter.Register<BuildMessageEventArgs>(BuildMessageEvent);
            m_converter.Register<BuildWarningEventArgs>(BuildWarningEvent);
            m_converter.Register<BuildErrorEventArgs>(BuildErrorEvent);
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
                // the context refers to the parent project, although it's a task who requests the build of the project
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
                Timestamp = e.Timestamp,

                ParentEventContext = parentContext,
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

            MessageEventContext context = null;
            if(e.BuildEventContext != BuildEventContext.Invalid)
            {
                int? projectId = null;
                if(e.BuildEventContext.ProjectInstanceId != BuildEventContext.InvalidProjectInstanceId)
                {
                    projectId = e.BuildEventContext.ProjectInstanceId;
                }

                int? targetId = null;
                if (e.BuildEventContext.TargetId != BuildEventContext.InvalidTargetId)
                {
                    targetId = e.BuildEventContext.TargetId;
                }

                int? taskId = null;
                if (e.BuildEventContext.TaskId != BuildEventContext.InvalidTaskId)
                {
                    taskId = e.BuildEventContext.TaskId;
                }

                context = new MessageEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = projectId,
                    TargetId = targetId,
                    TaskId = taskId,
                };
            }

            return new MessageEvent()
            {
                Context = context,
                Message = e.Message,
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

        // BuildWarningEventArgs => WarningEvent
        private Event BuildWarningEvent(BuildEventArgs buildEvent)
        {
            BuildWarningEventArgs e = buildEvent as BuildWarningEventArgs;
            Debug.Assert(e != null);

            MessageEventContext context = null;
            if (e.BuildEventContext != BuildEventContext.Invalid)
            {
                Debug.Assert(e.BuildEventContext.NodeId != BuildEventContext.InvalidNodeId);
                Debug.Assert(e.BuildEventContext.ProjectContextId != BuildEventContext.InvalidProjectContextId);

                int? projectId = null;
                if (e.BuildEventContext.ProjectInstanceId != BuildEventContext.InvalidProjectInstanceId)
                {
                    projectId = e.BuildEventContext.ProjectInstanceId;
                }

                int? targetId = null;
                if (e.BuildEventContext.TargetId != BuildEventContext.InvalidTargetId)
                {
                    targetId = e.BuildEventContext.TargetId;
                }

                int? taskId = null;
                if (e.BuildEventContext.TaskId != BuildEventContext.InvalidTaskId)
                {
                    taskId = e.BuildEventContext.TaskId;
                }

                context = new MessageEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = projectId,
                    TargetId = targetId,
                    TaskId = taskId,
                };
            }

            return new WarningEvent()
            {
                Context = context,
                Message = e.Message,
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

        // BuildErrorEventArgs => ErrorEvent
        private Event BuildErrorEvent(BuildEventArgs buildEvent)
        {
            BuildErrorEventArgs e = buildEvent as BuildErrorEventArgs;
            Debug.Assert(e != null);

            MessageEventContext context = null;
            if (e.BuildEventContext != BuildEventContext.Invalid)
            {
                Debug.Assert(e.BuildEventContext.NodeId != BuildEventContext.InvalidNodeId);
                Debug.Assert(e.BuildEventContext.ProjectContextId != BuildEventContext.InvalidProjectContextId);

                int? projectId = null;
                if (e.BuildEventContext.ProjectInstanceId != BuildEventContext.InvalidProjectInstanceId)
                {
                    projectId = e.BuildEventContext.ProjectInstanceId;
                }

                int? targetId = null;
                if (e.BuildEventContext.TargetId != BuildEventContext.InvalidTargetId)
                {
                    targetId = e.BuildEventContext.TargetId;
                }

                int? taskId = null;
                if (e.BuildEventContext.TaskId != BuildEventContext.InvalidTaskId)
                {
                    taskId = e.BuildEventContext.TaskId;
                }

                context = new MessageEventContext()
                {
                    NodeId = e.BuildEventContext.NodeId,
                    ContextId = e.BuildEventContext.ProjectContextId,
                    ProjectId = projectId,
                    TargetId = targetId,
                    TaskId = taskId,
                };
            }

            return new ErrorEvent()
            {
                Context = context,
                Message = e.Message,
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
