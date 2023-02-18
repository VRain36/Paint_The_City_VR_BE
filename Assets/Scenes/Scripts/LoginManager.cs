using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PaintTheCity
{
    public class LoginManager : MonoBehaviour
    {
        public GameObject loginPanel;

        public InputField ID_field;

        public Button LoginButton; 

        public static string user_id = "";

        public void LoginButtonClick()
        {
            user_id = ID_field.text;
            loginPanel.gameObject.SetActive(false);
            Debug.Log("[로그인] 현재 ID = " + user_id);
        }

        public void LoginRequestButtonClick()
        {
            loginPanel.gameObject.SetActive(true);
        }
    }
}