using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PaintTheCity
{
    /// <summary>
    /// [전체 시나리오]
    /// 1) Main Screen 에서, 작품 업로드 버튼을 클릭합니다.
    /// 2) 작품 카테고리 기반으로 작품의 이미지를 선택하는 창을 띄웁니다.
    /// 3) 사용자 닉네임, public/private 모드, 작품 이름을 설정하는 창을 띄웁니다.
    /// 4) 위의 창에서 Upload 버튼을 눌러 작품을 업로드합니다.
    /// 위의 시나리오에서 이 C# 스크립트는 1) -> 2)의 내용을 담고 있습니다.
    /// </summary>
    public class ArtimgManager : MonoBehaviour
    {
        public GameObject ArtimgPanel;
        public GameObject DonePanel;
        public Button privateButton;
        public InputField artwork_name_field;
        public static string artItemName = "";

        /// <summary>
        /// 시나리오에서 2) 창을 띄우는 함수입니다.
        /// </summary>
        public void ArtImgButtonClick()
        {
            ArtimgPanel.gameObject.SetActive(true);
        }

        /// <summary>
        /// 시나리오의 2) 창을 닫도록 하는 CANCEL 버튼 함수입니다.
        /// </summary>
        public void CancelButtonClick()
        {
            ArtimgPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 시나리오에서 2) -> 3)으로 넘어가도록 하는 OK 버튼 함수입니다.
        /// </summary>
        public void OKButtonClick()
        {
            ArtimgPanel.gameObject.SetActive(false); // 시나리오의 2) 창 닫기 
            DonePanel.gameObject.SetActive(true); // 시나리오의 3) 창 닫기

            artwork_name_field.text = ""; // 작품 이름 입력창을 빈 칸으로 초기화합니다.
        }

        /// <summary>
        /// 시나리오에서 2) 창에 관한 UI입니다.
        /// 선택된 카테고리 이미지에 표시 (뒷 배경이 연한 하늘색) 되도록 만들었습니다.
        /// </summary>
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