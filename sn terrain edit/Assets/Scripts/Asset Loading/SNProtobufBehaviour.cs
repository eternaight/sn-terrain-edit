using System.IO;
using ProtoBuf;
using UnityEngine;

namespace ReefEditor.ContentLoading {
    public class SNProtobufBehaviour : MonoBehaviour {
        SNTypeModel typeModel;

        void Start()
        {
            typeModel = new SNTypeModel();
            LoadObjects();
        }

        void LoadObjects() {
            string path = @"C:\Program Files (x86)\Steam\steamapps\common\Subnautica\Subnautica_Data\StreamingAssets\SNUnmanagedData\Build18\BatchObjectsCache";
            string filename = path + "\\batch-objects-12-18-12.bin";
            
            if (!File.Exists(filename)) {
                print("Failed to load objects file: no such directory.");
                return;
            }
            
            byte[] buffer = File.ReadAllBytes(filename);
            using (MemoryStream memStream = new MemoryStream(buffer, false)) {
                StreamHeader header = new StreamHeader();
                typeModel.DeserializeWithLengthPrefix(memStream, header, typeof(StreamHeader), PrefixStyle.Base128, 0);
                print(header);
            }
        }
    }

    [ProtoContract]
    public class StreamHeader {
		[ProtoMember(1)]
		public int Signature { get; set; }

		[ProtoMember(2)]
		public int Version { get; set; }

		public void Reset()
		{
			this.Signature = 0;
			this.Version = 0;
		}

		public override string ToString()
		{
			return string.Format("(UniqueIdentifier={0}, Version={1})", this.Signature, this.Version);
		}
    }
}