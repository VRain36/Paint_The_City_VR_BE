using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PaintTheCity
{
    public class ArtimgManager : MonoBehaviour
    {
        public GameObject ArtimgPanel;
        public GameObject DonePanel;

        public Button privateButton;

        public InputField artwork_name_field;

        public static string artItemName = "";

        public void ArtImgButtonClick()
        {
            ArtimgPanel.gameObject.SetActive(true);
        }

        public void CancelButtonClick()
        {
            ArtimgPanel.gameObject.SetActive(false);
        }

        public void OKButtonClick()
        {
            ArtimgPanel.gameObject.SetActive(false);
            DonePanel.gameObject.SetActive(true);

            artwork_name_field.text = ""; 

            if (LoginManager.user_id == "")
            {
                privateButton.gameObject.SetActive(false);
            }
            else
            {
                privateButton.gameObject.SetActive(true);
            }
        }

        public void ArtItemButtonClick() 
        {
            // 방금 클릭한 게임 오브젝트의 번호 (= 작품 번호) 저장
            GameObject clickObject = EventSystem.current.currentSelectedGameObject;

            // 클릭된 버튼에만 이미지 띄우기
            string currentItemName = clickObject.name;

            if (artItemName != "")
            {
                GameObject.Find(artItemName).transform.Find("OnBackground").gameObject.SetActive(false);
            }

            GameObject.Find(currentItemName).transform.Find("OnBackground").gameObject.SetActive(true);

            // 현재 클릭된 버튼 이름 업데이트
            artItemName = currentItemName;
        }
    }
}