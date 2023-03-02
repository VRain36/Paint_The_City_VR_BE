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
    /// <summary>
    /// [전체 시나리오]
    /// 1) Main Screen 에서, 작품 업로드 버튼을 클릭합니다.
    /// 2) 작품 카테고리 기반으로 작품의 이미지를 선택하는 창을 띄웁니다.
    /// 3) 사용자 닉네임, public/private 모드, 작품 이름을 설정하는 창을 띄웁니다.
    /// 4) 위의 창에서 Upload 버튼을 눌러 작품을 업로드합니다.
    /// 위의 시나리오에서 이 C# 스크립트는 3) -> 4)의 내용을 담고 있습니다.
    /// </summary>
    public class UploadArtwork : MonoBehaviour
    {
        public GameObject DonePanel;
        public GameObject UploadDonePanel;
        public Button privateButton;
        public InputField user_id_field;
        public InputField artwork_name_field;
        public int public_mode = 1;
        public static string date_time = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
        public string bucketName = "ptc-s3-bucket";
        public string fbx_directory = "";
        public string obj_directory = "";
        public string fbx_file = "";
        public string obj_file = "";
        public string mtl_file = "";
        public bool objDone = false;
        public RawImage publicUncheck;
        public RawImage privateUncheck;
        public RawImage publicCheck;
        public RawImage privateCheck;

        /// <summary>
        /// fbx -> obj, mtl 파일로 변환하기 위해서 저장 경로를 설정합니다.
        /// 1) fbx_directory 아래에 texture 폴더가 존재함을 전제합니다.
        /// 2) obj 파일 이름이 중복되는 것을 방지하기 위해서, datetime으로 폴더명을 지정합니다.
        /// </summary>
        public void Update()
        {
            obj_directory = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time;
            obj_file = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time + "/artwork.obj";
            mtl_file = System.Environment.CurrentDirectory + "/Assets/OBJFiles/" + date_time + "/artwork.mtl";
        }

        /// <summary>
        /// 사용자 닉네임 OK 버튼 클릭하면 실행되는 함수입니다.
        /// </summary>
        public void onIDOKButtonClick()
        {
            if (user_id_field.text == "")
            {
                privateButton.gameObject.SetActive(false);
            }
            else
            {
                privateButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// public 모드 버튼 함수입니다.
        /// </summary>
        public void publicButtonClick()
        {
            public_mode = 1;
            publicCheck.gameObject.SetActive(true);
            privateUncheck.gameObject.SetActive(true);
            publicUncheck.gameObject.SetActive(false);
            privateCheck.gameObject.SetActive(false);
        }

        /// <summary>
        /// private 모드 버튼 함수입니다.
        /// </summary>
        public void privateButtonClick()
        {
            public_mode = 0;
            publicCheck.gameObject.SetActive(false);
            privateUncheck.gameObject.SetActive(false);
            publicUncheck.gameObject.SetActive(true);
            privateCheck.gameObject.SetActive(true);
        }

        /// <summary>
        /// 시나리오의 3) 창을 닫도록 하는 CANCEL 버튼 함수입니다.
        /// </summary>
        public void CancelButtonClick()
        {
            DonePanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 시나리오의 4) 창을 닫도록 하는 CANCEL 버튼 함수입니다.
        /// </summary>
        public void CloseButtonClick()
        {
            UploadDonePanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 로컬의 fbx 파일을 obj 파일로 변환하여 AWS의 S3 스토리지에 업로드하는 함수입니다.
        /// 3) 창에서 Upload 버튼을 클릭하면 실행됩니다.
        /// 1) fbx -> obj 파일 변환
        /// 2) obj, mtl, textures 파일 업로드 
        /// 3) 데이터베이스 업데이트
        /// 위의 과정은 확인하기 편하도록 <summary> 단위로 작성하였습니다.
        /// </summary>
        public void UploadButtonClick()
        {
            /// <summary>
            /// 1) fbx -> obj 파일 변환
            /// </summary>
            fbx_directory = System.Environment.CurrentDirectory + "/Assets/FBXFiles/";
            Debug.Log(fbx_directory); // 로컬에서 fbx 파일이 저장된 경로를 확인하기 위한 용도
            convertFbxToObj(); // fbx -> obj 파일 변환             
            while (true) // 폴더 내에 obj 파일 생성되었는지 확인 -> 확인되면 S3에 업로드 시작 
            {
                if (File.Exists(obj_file) == true)
                {
                    break;
                }
            }

            /// <summary>
            /// 2) obj, mtl, textures 파일 업로드 
            /// </summary>
            if (artwork_name_field.text == "") // 작품 이름을 입력하지 않는 경우, 기본값 지정
            {
                artwork_name_field.text = "artwork";
            }
            string folderName = artwork_name_field.text + " " + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + "/"; // 작품 이름이 중복되지 않도록 날짜/시간 붙이기 
            string filePath = obj_file; 
            folderName = folderName.Replace(" ", "-");
            UploadToS3(filePath, folderName);

            /// <summary>
            /// 3) 데이터베이스 업데이트
            /// API output 형식 : user_id=id&artimg_url=url&artwork_name=name&public_mode=0
            /// </summary>
            if (user_id_field.text == "") // 사용자 닉네임을 입력하지 않는 경우, 기본값 지정
            {
                user_id_field.text = "PaintTheCity_User";
                public_mode = 1; // 사용자 닉네임을 입력하지 않은 경우, public 모드만 설정 가능 
            }
            string user_id = user_id_field.text;
            string artimg_url = ArtimgManager.artItemName;
            string artwork_name = artwork_name_field.text;
            string artwork_url = folderName;
            StartCoroutine(UpdateDB(user_id, artimg_url, artwork_name, artwork_url, public_mode));
            DonePanel.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 1) fbx -> obj 파일 변환하는 함수입니다.
        /// </summary>
        public void convertFbxToObj()
        {
            /*
            Aspose.ThreeD.License Aspose3DLicense = new Aspose.ThreeD.License();
            Aspose3DLicense.SetLicense(@"c:\asposelicense\license.lic");
            */
            DirectoryInfo obj_directory_info = new DirectoryInfo(obj_directory);
            DirectoryInfo dirInfo = new DirectoryInfo(fbx_directory); 
            FileInfo[] files = dirInfo.GetFiles("*.fbx"); 

            if(obj_directory_info.Exists == false) // obj, mtl 저장하는 폴더 생성 
            {
                obj_directory_info.Create(); 
            }

            foreach(FileInfo file in files) // 업로드 할 fbx 파일 찾기 
            {
                fbx_file = fbx_directory + file.Name; 
                Debug.Log("[Fbx file] " + fbx_file);
                break;
            }

            if (fbx_file != "") // fbx -> obj 변환 
            {
                Scene FBX3DScene = new Scene(); //Create a object of type 3D Scene to hold and convert FBX file
                FBX3DScene.Open(fbx_file);
                FBX3DScene.Save(obj_file, FileFormat.WavefrontOBJ); //Save the output as Wavefront OBJ 3D file format
            }
        }

        /// <summary>
        /// 2) obj, mtl 파일 업로드를 위한 AWS 설정 관련 부분입니다.
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

        /// <summary>
        /// 2) obj, mtl 파일 업로드 함수입니다.
        /// </summary>
        public void UploadToS3(string filePath, string folderName)
        {
            // (1) obj 파일 업로드 
            UploadFile(obj_file, folderName + "artwork.obj");
            // (2) mtl 파일 업로드
            UploadFile(mtl_file, folderName + "artwork.mtl"); 
            // (3) textures 폴더 내 파일 업로드 
            UploadFolder(fbx_directory + "/textures", folderName);
        }

        /// <summary>
        /// 2) obj, mtl 파일 업로드 함수입니다.
        /// i) '파일' 단위로 업로드하는 경우 
        /// </summary>
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

        /// <summary>
        /// 2) obj, mtl 파일 업로드 함수입니다.
        /// ii) '폴더' 단위로 업로드하는 경우 
        /// </summary>
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
        /// 3) 데이터베이스 업데이트하는 함수입니다.
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

            if (www.downloadHandler.text == "Updating artlist info complete")
            {
                UploadDonePanel.gameObject.SetActive(true);
            }
            www.Dispose();
        }
    }

}
