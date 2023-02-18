using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.S3.Util;
using Amazon;
using Amazon.CognitoIdentity;
using Aspose.ThreeD;

namespace PaintTheCity
{
    public class UploadArtwork : MonoBehaviour
    {
        public GameObject DonePanel;
        public GameObject UploadDonePanel;

        public Button privateButton;

        public InputField artwork_name_field;

        public int public_mode = 1;

        /// <summary>
        /// 1) fbx_directory 아래에 texture 폴더가 존재함을 전제
        /// 2) obj 파일 이름이 중복되는 것을 방지하기 위해서, datetime으로 폴더명 지정
        /// </summary>
        public string bucketName = "ptc-s3-bucket";
        public string fbx_file = "";
        public static string date_time = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
        public string fbx_directory = System.Environment.CurrentDirectory + "/Assets/FBXFiles/";
        public string obj_directory = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time;
        public string obj_file = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time + "/artwork.obj";
        public string mtl_file = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time + "/artwork.mtl";
        
        public bool objDone = false;

        public RawImage publicUncheck;
        public RawImage privateUncheck;
        public RawImage publicCheck;
        public RawImage privateCheck;

        public void Update()
        {
            obj_directory = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time;
            obj_file = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time + "/artwork.obj";
            mtl_file = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time + "/artwork.mtl";
        }

        public void publicButtonClick()
        {
            public_mode = 1;
            publicCheck.gameObject.SetActive(true);
            privateUncheck.gameObject.SetActive(true);
            publicUncheck.gameObject.SetActive(false);
            privateCheck.gameObject.SetActive(false);
        }

        public void privateButtonClick()
        {
            public_mode = 0;
            publicCheck.gameObject.SetActive(false);
            privateUncheck.gameObject.SetActive(false);
            publicUncheck.gameObject.SetActive(true);
            privateCheck.gameObject.SetActive(true);
        }

        public void CancelButtonClick()
        {
            DonePanel.gameObject.SetActive(false);
        }

        public void CloseButtonClick()
        {
            UploadDonePanel.gameObject.SetActive(false);
        }

        public void UploadButtonClick()
        {
            // 1) fbx -> obj 파일 변환 
            convertFbxToObj();

            // 폴더 내에 obj 파일 생성되었는지 확인 -> 확인되면 S3에 업로드 시작 
            while (true)
            {
                if (File.Exists(obj_file) == true)
                {
                    break;
                }
            }

            // 2) obj, mtl, textures 파일 업로드 
            if (artwork_name_field.text == "")
            {
                artwork_name_field.text = "artwork";
            }

            string folderName = artwork_name_field.text + " " + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + "/"; // 작품 이름이 중복되지 않도록 날짜/시간 붙이기 
            string filePath = obj_file; 
            folderName = folderName.Replace(" ", "-");
            UploadToS3(filePath, folderName);

            // 3) 데이터베이스 업데이트
            // user_id=id&artimg_url=url&artwork_name=name&public_mode=0
            string user_id = "";
            if (LoginManager.user_id == "")
            {
                user_id = "PaintTheCity_User";
            }
            else
            {
                user_id = LoginManager.user_id;
            }
            
            string artimg_url = ArtimgManager.artItemName;

            string artwork_name = artwork_name_field.text;

            string artwork_url = folderName;
            
            if (user_id == "PaintTheCity_User")
            {
                public_mode = 1;
            }

            StartCoroutine(UpdateDB(user_id, artimg_url, artwork_name, artwork_url, public_mode));

            DonePanel.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 1) fbx -> obj 파일 변환
        /// </summary>
        public void convertFbxToObj()
        {
            // Aspose.ThreeD.License Aspose3DLicense = new Aspose.ThreeD.License();
            // Aspose3DLicense.SetLicense(@"c:\asposelicense\license.lic");
        
            DirectoryInfo dirInfo = new DirectoryInfo(fbx_directory);       //Assuming Test is your Folder
            FileInfo[] files = dirInfo.GetFiles("*.fbx"); 

            DirectoryInfo obj_directory_info = new DirectoryInfo(obj_directory);

            // obj, mtl 저장하는 폴더 생성 
            if(obj_directory_info.Exists == false)
            {
                obj_directory_info.Create(); 
            }

            foreach(FileInfo file in files)
            {
                fbx_file = fbx_directory + file.Name; 
                Debug.Log("[Fbx file] " + fbx_file);
                break;
            }

            if (fbx_file != "")
            {
                //Create a object of type 3D Scene to hold and convert FBX file
                Scene FBX3DScene = new Scene();
                FBX3DScene.Open(fbx_file);

                //Save the output as Wavefront OBJ 3D file format
                FBX3DScene.Save(obj_file, FileFormat.WavefrontOBJ);
            }
        }

        /// <summary>
        /// 2) obj, mtl 파일 업로드 
        /// </summary>
        private string S3Region = RegionEndpoint.APNortheast2.SystemName;

        private RegionEndpoint _S3Region
        {
            get 
            {
                return RegionEndpoint.GetBySystemName(S3Region);
            }
        }

        private AmazonS3Client _s3Client;

        public AmazonS3Client S3Client
        {
            get
            {
                if (_s3Client == null)
                {
                    _s3Client = new AmazonS3Client(new CognitoAWSCredentials(
                    "[TO-DO 1]", // Identity pool ID
                    RegionEndpoint.APNortheast2 // Region
                    ), _S3Region);
                }
                return _s3Client;
            }
        }

        public void UploadToS3(string filePath, string folderName)
        {
            // (1) obj 파일 업로드 
            UploadFile(obj_file, folderName + "artwork.obj");
            // (2) mtl 파일 업로드
            UploadFile(mtl_file, folderName + "artwork.mtl"); 
            // (3) textures 폴더 내 파일 업로드 
            UploadFolder(fbx_directory + "/textures", folderName);
        }

        public async void UploadFile(string filePath, string fileName)
        {
            string objectName = "ptc-artwork/" + fileName;
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectName,
                InputStream = stream
            };

            var response = await S3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                Debug.Log("Successfully uploaded " + objectName + " to " + bucketName);
            }
            else
            {
                Debug.Log("Could not upload " + objectName + " to " + bucketName);
            }
        }

        public async void UploadFolder(string folderPath, string uploadFolderPath)
        {
            string[] file_exts = new string[]{"jpg", "png"};

            string[] textureFiles = Directory.GetFiles(folderPath);
            foreach (string textureFile in textureFiles)
            {
                string[] temp_file_name = textureFile.Split(new char[] { '.' });
                string ext = temp_file_name[temp_file_name.Length-1];

                foreach (string file_ext in file_exts)
                {
                    if (ext == file_ext)
                    {
                        string[] temp_name = textureFile.Split(new char[] {'\\'});
                        string fileName = temp_name[temp_name.Length-1];
                        string objectName = "ptc-artwork/" + uploadFolderPath + fileName;

                        FileStream stream = new FileStream(textureFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                        var request = new PutObjectRequest
                        {
                            BucketName = bucketName,
                            Key = objectName,
                            InputStream = stream
                        };

                        var response = await S3Client.PutObjectAsync(request);

                        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                        {
                            Debug.Log("Successfully uploaded " + objectName + " to " + bucketName);
                        }
                        else
                        {
                            Debug.Log("Could not upload " + objectName + " to " + bucketName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 3) 데이터베이스 업데이트
        /// </summary>
        IEnumerator UpdateDB(string user_id, string artimg_url, string artwork_name, string artwork_url, int public_mode) 
        {
            // user_id=id&artimg_url=url&artwork_name=name&public_mode=0
            WWWForm form = new WWWForm();
            form.AddField("user_id", user_id);
            form.AddField("artimg_url", artimg_url);
            form.AddField("artwork_name", artwork_name);
            form.AddField("artwork_url", artwork_url);
            form.AddField("public_mode", public_mode);

            string artwork_API_url = "[TO-DO 2]";
            UnityWebRequest www = UnityWebRequest.Post(artwork_API_url, form);

            yield return www.SendWebRequest();
            Debug.Log("[RDS 업데이트] " + www.downloadHandler.text);
            
            if (www.downloadHandler.text == "Updating artlist info complete")
            {
                UploadDonePanel.gameObject.SetActive(true);
            }

            www.Dispose();
        }
    }

}
