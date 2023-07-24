using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using MySql.Data.MySqlClient;

namespace MyButtonLibrary
{
    public class MyButtonEventHandler
    {
        private Dictionary<Button, (Brush background, object content)> buttonStates;

        public MyButtonEventHandler()
        {
            // 이전 상태를 저장할 Dictionary 초기화
            buttonStates = new Dictionary<Button, (Brush background, object content)>();
        }

        public void HandleButtonMouseEnter(object sender)
        {
            Button button = (Button)sender;
            // 원래 배경색과 컨텐츠를 저장합니다.
            if (!buttonStates.ContainsKey(button))
                buttonStates.Add(button, (button.Background, button.Content));
        }

        public void HandleButtonMouseLeave(Button button)
        {
            // 원래 배경색과 컨텐츠로 복원합니다.
            if (button.Name == "BtnNoCrownCheak" && buttonStates.TryGetValue(button, out var red))
            {
                button.Content = "완료!";
                button.Background = red.background;
            }
            else if (button.Name == "BtnClearCrownCheak" && buttonStates.TryGetValue(button, out var silver))
            {
                button.Content = "완료!";
                button.Background = silver.background;
            }
            else if (button.Name == "BtnFullComboCrownCheak" && buttonStates.TryGetValue(button, out var gold))
            {
                button.Content = "완료!";
                button.Background = gold.background;
            }
        }
    }
}