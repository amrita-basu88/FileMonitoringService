using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using FileMonitorService.Models;
using LoggerSingleton;
using Newtonsoft.Json;

namespace WebDashboard
{
    public class WebDashboardLogic
    {
        private static readonly String IgnoreDirectoryIsilon = ConfigurationManager.AppSettings["IgnoreDirectoryIsilon"];
        private static readonly string DashBoardDataFilename = ConfigurationManager.AppSettings["DashBoardDataFilename"];

        private static String lastprojectsJson = String.Empty;

        public static void CreateHTML(IEnumerable<Subscription> subscriptions)
        {
            Projects projects = new Projects { ProjectsDictionary = new Dictionary<string, Project>() };

            foreach (var subscription in subscriptions)
            {
                switch (subscription.InvokeMethodData.MethodName)
                {
                    case "ChangeInStorageRoot": // Project
                        {
                            UpdateProjects(subscription, projects);
                        }
                        break;
                    case "ChangeInCamSerialDirectory": // Camera
                        {
                            UpdateCameras(subscription, projects);
                        }
                        break;
                    case "FoundNewTsFolder": // Recording
                        {
                            UpdateRecordings(subscription, projects);
                        }
                        break;
                }
            }

            CreateDashBoardJson(projects);
        }

        private static void UpdateProjects(Subscription subscription, Projects projects)
        {
            foreach (var networkFile in subscription.fileIndex)
            {
                String projectName = Path.GetFileName(networkFile.Path);
                if (projectName == null || projects.ProjectsDictionary.ContainsKey(projectName))
                {
                    continue;
                }

                if (projectName.Equals(IgnoreDirectoryIsilon))
                {
                    continue;
                }

                projects.ProjectsDictionary.Add(projectName, new Project
                {
                    ProjectName = projectName,
                    CreateDate = networkFile.ModificationDate,
                });
            }
        }

        private static void UpdateCameras(Subscription subscription, Projects projects)
        {
            string projectName = Path.GetFileName(subscription.Path);
            if (projectName == null || !projects.ProjectsDictionary.ContainsKey(projectName))
            {
                return;
            }
            Dictionary<String, Camera> cameras = projects.ProjectsDictionary[projectName].Cameras;
            foreach (var networkFile in subscription.fileIndex)
            {
                String cameraName = Path.GetFileName(networkFile.Path);
                if (cameraName == null)
                {
                    continue;
                }
                if (cameras.ContainsKey(cameraName))
                {
                    if (cameras[cameraName].CreateDate.Equals(DateTime.MinValue))
                    {
                        cameras[cameraName].CreateDate = networkFile.ModificationDate;
                    }
                }
                else
                {
                    cameras.Add(cameraName, new Camera
                    {
                        CameraName = cameraName,
                        CreateDate = networkFile.ModificationDate,
                    });
                }
            }
        }

        private static void UpdateRecordings(Subscription subscription, Projects projects)
        {
            String projectName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(subscription.Path)));
            String cameraName = Path.GetFileName(Path.GetDirectoryName(subscription.Path));

            if (projectName == null || !projects.ProjectsDictionary.ContainsKey(projectName))
            {
                return;
            }

            Dictionary<String, Camera> cameras = projects.ProjectsDictionary[projectName].Cameras;
            if (cameraName == null)
            {
                return;
            }

            if (!cameras.ContainsKey(cameraName))
            {
                cameras.Add(cameraName, new Camera
                {
                    CameraName = cameraName
                });
            }

            Dictionary<String, Recording> recordings = cameras[cameraName].Recordings;
            foreach (var networkFile in subscription.fileIndex)
            {
                String recordingName = Path.GetFileName(networkFile.Path);
                if (recordingName == null)
                {
                    continue;
                }

                recordingName = recordingName.Replace(".TSFOLDER", "");
                String completedClipXml = networkFile.Path.Replace(".TSFOLDER", ".XML");
                if (!recordings.ContainsKey(recordingName))
                {
                    recordings.Add(recordingName, new Recording
                    {
                        RecordingName = recordingName,
                        CreateDate = networkFile.ModificationDate,
                        RecordingState = File.Exists(completedClipXml) ? RecordingStateEnum.Completed : RecordingStateEnum.Active
                    });
                }
                else if (recordings[recordingName].RecordingState == RecordingStateEnum.Active)
                {
                    if (File.Exists(completedClipXml))
                    {
                        recordings[recordingName].RecordingState = RecordingStateEnum.Completed;
                    }
                }
            }
        }

        private static void CreateDashBoardJson( Projects projects )
        {
            if (projects == null)
            {
                return;
            }

            try
            {
                Int32 id = 1;
                List<ReactGridRowItem> items = new List<ReactGridRowItem>();
                foreach (var project in projects.ProjectList)
                {
                    foreach (var camera in project.CamerasList)
                    {
                        foreach (var recording in camera.RecordingList)
                        {
                            items.Add(new ReactGridRowItem
                            {
                                id = id,
                                project = InsertStringEveryPosition(project.ProjectName, 30, Environment.NewLine),
                                camera = camera.CameraName,
                                recording = recording.RecordingName,
                                recordingstate = recording.RecordingState.ToString(),
                                recordingdate = recording.CreateDate.ToString("yyyy-MMM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                            });
                            id++;
                        }
                    }
                }

                String projectsJson = JsonConvert.SerializeObject(items, Formatting.Indented);
                if (projectsJson.Equals(lastprojectsJson))
                {
                    return;
                }
                lastprojectsJson = projectsJson;

                if (File.Exists(DashBoardDataFilename))
                {
                    SingletonLogger.Instance.Info(String.Format("Updating dashboard data file='{0}'", DashBoardDataFilename));


                    String jsonp = String.Format("var fakeData = {0};", projectsJson);
                    File.WriteAllText(DashBoardDataFilename, jsonp, Encoding.UTF8);
                }
                else
                {
                    SingletonLogger.Instance.Error(String.Format("Dashboard data file='{0} does not exists.", DashBoardDataFilename));
                }
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Unhandled exception in CreateDashBoardJson.{0}Exception={1}", 
                    Environment.NewLine, ex));
            }
        }

        private static String InsertStringEveryPosition(String input, Int32 position, String insertValue)
        {
            StringBuilder sb = new StringBuilder(input);
            for (Int32 i = (input.Length / position) * position; i >= position; i -= position)
            {
                sb.Insert(i, insertValue);
            }
            sb.Append(insertValue);
            return sb.ToString();
        }
    }
}
