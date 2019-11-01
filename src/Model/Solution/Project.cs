using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class Project : DiskFile
    {
        public string TypeUUID { get; }
        public string Name { get; }
        public string UUID { get; }
        public List<Project> DependsOn { get; }

        public Project(string path, string typeUUID, string name, string uuid)
        {
            Path = path;
            TypeUUID = typeUUID;
            Name = name;
            UUID = uuid;

            DependsOn = new List<Project>();
        }
    }
}
