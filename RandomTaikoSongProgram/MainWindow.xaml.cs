using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using RandomTaikoSongProgram.Logics;
using RandomTaikoSongProgram.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.IconPacks;

namespace RandomTaikoSongProgram
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        bool isFavorite = false; // false -> openApi 검색해온결과, true -> 즐겨찾기 보기
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtYouTubeName.Focus(); // 텍스트박스에 포커스 셋
        }

        // 세부 설정 버튼
        private void BtnOffset_Click(object sender, RoutedEventArgs e) 
        {

        }

        // 랜덤 추첨
        private void BtnRandomSong_Click(object sender, RoutedEventArgs e) 
        {

        }

        // 찾은거 유튜브로 보기 (완료)
        private async void BtnWatchTrailer_Click(object sender, RoutedEventArgs e) 
        {
            if (GrdResult.SelectedItems.Count == 0)
            {
                await Commons.ShowMessageAsync("유튜브", "노래를 선택하세요.");
                return;
            }
            if (GrdResult.SelectedItems.Count > 1)
            {
                await Commons.ShowMessageAsync("유튜브", "노래를 하나만 선택하세요.");
                return;
            }

            string songName = string.Empty;
            if (GrdResult.SelectedItem is YSI)
            {
                var song = GrdResult.SelectedItem as YSI;
                songName = song.Title;
            }
            else if (GrdResult.SelectedItem is YoutubeSongItem)
            {
                var song = GrdResult.SelectedItem as YoutubeSongItem;
                songName = song.Title;
            }
            //await Commons.ShowMessageAsync("유튜브", $"노래 검색 {songName}");
            var trailerWindow = new TrailerWindow(songName)
            {
                Owner = this, // TrailerWindow의 부모는 MainWindow
                WindowStartupLocation = WindowStartupLocation.CenterOwner // 부모창의 정중앙에 위치
            };
            //trailerWindow.Show(); // 모달리스로 창을 열면 부모차을 손댈 수 있음
            trailerWindow.ShowDialog(); // 모달창

        }

        // 검색버튼, OpenAPI 노래 검색 (완료)
        private async void BtnSearchYouTube_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtYouTubeName.Text))
            {
                await Commons.ShowMessageAsync("검색", "검색할 노래명을 입력하세요.");
                return;
            }

            //if (TxtYouTubeName.Text.Length <= 2)
            //{
            //    await Commons.ShowMessageAsync("검색", "검색어를 2자이상 입력하세요.");
            //    return;
            //}

            try
            {
                SearchYouTube(TxtYouTubeName.Text);
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("오류", $"오류 발생 : {ex.Message}");
            }
        }

        // 실제 검색메서드
        private async void SearchYouTube(string SongName)
        {
            string tmdb_apiKey = "2afb7029a3cbdcaf0f965bf5f7e5c97a";
            string encoding_movieName = HttpUtility.UrlEncode(SongName, Encoding.UTF8);
            string openApiUri = $"https://api.themoviedb.org/3/search/movie?api_key={tmdb_apiKey}" +
                                $"&language=ko-KR&page=1&include_adult=false&query={encoding_movieName}"; // 노래 검색 URL
            string result = string.Empty; // 결과값

            // api 실행할 객체
            WebRequest req = null;
            WebResponse res = null;
            StreamReader reader = null;
                        
            // result를 json으로 변경
            var jsonResult = JObject.Parse(result); // string -> json

            var total = Convert.ToInt32(jsonResult["total_results"]); // 전체 검색결과 수
            //await Commons.ShowMessageAsync("검색결과", total.ToString());
            var items = jsonResult["results"];
            // items를 데이터그리드에 표시
            var json_array = items as JArray;

            var songItems = new List<YSI>(); // json에서 넘어온 배열을 담을 장소
            foreach (var val in json_array)
            {
                var songItem = new YSI()
                {
                    Title = Convert.ToString(val["title"]),
                    Composer = Convert.ToString(val["composer"]),
                    Difficulty = Convert.ToString(val["difficulty"]),
                    Genre = Convert.ToString(val["genre"]),
                    Youtube = Convert.ToString(val["youtube"])
                };
                songItems.Add(songItem);
            }

            this.DataContext = songItems;
            isFavorite = false; // 즐겨찾기가 아님.
            StsResult.Content = $"OpenAPI 조회 {songItems.Count} 건 조회 완료";
        }

        // 엔터로 검색창 넘어가게 이벤트 넣기
        private void TxtYouTubeName_KeyDown(object sender, KeyEventArgs e) 
        {
            if (e.Key == Key.Enter)
            {
                BtnSearchYouTube_Click(sender, e);
            }
        }

        // ----------------------- 즐겨찾기 목록 이벤트 핸들러 -------------------------

        private async void BtnAddFavorite_Click(object sender, RoutedEventArgs e) // 즐겨찾기 추가
        {
            List<YoutubeSongItem> list = new List<YoutubeSongItem>();
            if (GrdResult.SelectedItems.Count == 0)
            {
                await Commons.ShowMessageAsync("오류", "즐겨찾기에 추가할 노래를 선택하세요(복수선택 가능)");
                return;
            }

            if (isFavorite)
            {
                await Commons.ShowMessageAsync("오류", "이미 즐겨찾기한 노래입니다.");
                return;
            }

            #region < MySQL 연결 >
            try
            {
                // DB 연결확인
                using (MySqlConnection conn = new MySqlConnection(Commons.myConnString))
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    var query = @"INSERT INTO YoutubeSongItem
                                               ( Title
                                               , Composer
                                               , Difficulty
                                               , Genre
                                               , Youtube
                                               , Release )
                                         VALUES
                                               ( @Title
                                               , @Composer
                                               , @Difficulty
                                               , @Genre
                                               , @Youtube
                                               , @Release ) ";

                    var insRes = 0;
                    foreach (YoutubeSongItem item in list)
                    {
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Title", item.Title);
                        cmd.Parameters.AddWithValue("@Original_Title", item.Composer);
                        cmd.Parameters.AddWithValue("@Release_Date", item.Difficulty);
                        cmd.Parameters.AddWithValue("@Original_Language", item.Genre);
                        cmd.Parameters.AddWithValue("@Adult", item.Youtube);
                        cmd.Parameters.AddWithValue("@Reg_Date", DateTime.Now);

                        insRes += cmd.ExecuteNonQuery();
                    }

                    if (list.Count == insRes)
                    {
                        await Commons.ShowMessageAsync("저장", "DB저장성공");
                    }
                    else
                    {
                        await Commons.ShowMessageAsync("저장", "DB저장오류 관리자에게 문의하세요.");
                    }
                }
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("오류", $"DB저장 오류{ex.Message}");
            }
            #endregion
        }

        private async void BtnViewFavorite_Click(object sender, RoutedEventArgs e) // 즐겨찾기 목록
        {
            List<YoutubeSongItem> list = new List<YoutubeSongItem>();
            this.DataContext = null;
            TxtYouTubeName.Text = string.Empty;

            #region < MySQL 연결 >
            try
            {
                // DB 연결확인
                using (MySqlConnection conn = new MySqlConnection(Commons.myConnString))
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    var query = @"SELECT Title
                              , Composer
                              , Difficulty
                              , Genre
                              , Youtube
                              , Release 
                           FROM YoutubeSongItem
                           ORDER BY Title ASC";

                    // 쿼리 실행
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 결과에서 필요한 정보를 추출하여 YoutubeSongItem 객체를 생성하고 list에 추가
                            YoutubeSongItem item = new YoutubeSongItem
                            {
                                Title = reader["Title"].ToString(),
                                Composer = reader["Composer"].ToString(),
                                Difficulty = reader["Difficulty"].ToString(),
                                Genre = reader["Genre"].ToString(),
                                Youtube = reader["Youtube"].ToString(),
                                Release = Convert.ToDateTime(reader["Release"])
                            };

                            list.Add(item);
                        }
                    }

                    // list에 저장된 즐겨찾기 목록을 활용하여 필요한 작업 수행
                    // 예: 데이터 바인딩, UI에 출력 등

                    await Commons.ShowMessageAsync("조회", "DB 조회 성공");
                }
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("오류", $"DB 조회 오류: {ex.Message}");
            }
            #endregion
        }


        private async void BtnDelFavorite_Click(object sender, RoutedEventArgs e) // 즐겨찾기 제거
        {
            _ = new List<YoutubeSongItem>(); // List<YoutubeSongItem> list = new List<YoutubeSongItem>();
            if (isFavorite == false)
            {
                await Commons.ShowMessageAsync("오류", "즐겨찾기만 삭제할 수 있습니다.");
                return;
            }
            if (GrdResult.SelectedItems.Count == 0)
            {
                await Commons.ShowMessageAsync("오류", "삭제할 영화를 선택하세요.");
                return;
            }

            #region < MySQL 연결 >
            try // 삭제
            {
                using (MySqlConnection conn = new MySqlConnection(Commons.connString))
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    var query = "DELETE FROM YoutubeSongItem WHERE Title = @Title";
                    var delRes = 0;

                    foreach (YoutubeSongItem item in GrdResult.SelectedItems)
                    {
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Title", item.Title);

                        delRes += cmd.ExecuteNonQuery();
                    }

                    if (delRes == GrdResult.SelectedItems.Count)
                    {
                        await Commons.ShowMessageAsync("삭제", "노래삭제성공");
                        StsResult.Content = $"즐겨찾기 {delRes} 건 삭제완료";
                    }
                    else
                    {
                        await Commons.ShowMessageAsync("삭제", "노래 삭제 일부성공"); // 발생할일이 거의 없음

                    }
                }
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("오류", $"노래 삭제 오류{ex.Message}");
            }
            #endregion

            BtnViewFavorite_Click(sender, e); // 즐겨찾기 보기 이벤트핸들러를 한번 실행
        }

        // ----------------------- 왕관 체크 이벤트 핸들러 -------------------------       

        // ----------------------- 미클리어 이벤트 핸들러 -------------------------      
        private void BtnNoCrownCheak_Click(object sender, RoutedEventArgs e) // 버튼이 클릭되었을 때의 동작
        {
            BtnNoCrownCheak.Content = "완료!";
        }

        private void BtnNoCrownCheak_MouseEnter(object sender, MouseEventArgs e) // 마우스가 버튼에 들어갔을 때의 동작
        {
            // 버튼의 배경색을 연한 갈색(브라운)으로 변경.
            BtnNoCrownCheak.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
            BtnNoCrownCheak.Background = Brushes.Tan;
        }

        private void BtnNoCrownCheak_MouseLeave(object sender, MouseEventArgs e) // 마우스가 버튼을 떠날 때의 동작
        {            
            // 버튼의 배경색을 원래대로 복구
            BtnNoCrownCheak.Background = Brushes.Brown;
        }

        // ----------------------- 클리어 이벤트 핸들러 -------------------------      
        private void BtnClearCrownCheak_Click(object sender, RoutedEventArgs e)
        {
            BtnNoCrownCheak.Content = "완료!";
        }

        private void BtnClearCrownCheak_MouseEnter(object sender, MouseEventArgs e)
        {
            // 버튼의 배경색을 연한 회색으로 변경.
            BtnNoCrownCheak.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
            BtnNoCrownCheak.Background = Brushes.LightGray;
        }

        private void BtnClearCrownCheak_MouseLeave(object sender, MouseEventArgs e)
        {
            BtnNoCrownCheak.Background = Brushes.Gray;
        }

        // ----------------------- 풀콤보 이벤트 핸들러 -------------------------
        private void BtnFullComboCrownCheak_Click(object sender, RoutedEventArgs e)
        {
            BtnNoCrownCheak.Content = "완료!";
        }

        private void BtnFullComboCrownCheak_MouseEnter(object sender, MouseEventArgs e)
        {
            // 버튼의 배경색을 연한 노랑으로 변경.
            BtnNoCrownCheak.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
            BtnNoCrownCheak.Background = Brushes.LightGoldenrodYellow;
        }

        private void BtnFullComboCrownCheak_MouseLeave(object sender, MouseEventArgs e)
        {
            BtnNoCrownCheak.Background = Brushes.Gold;
        }

        // ----------------------- 퍼펙트 이벤트 핸들러 -------------------------
        private void BtnAllPerpectCrownCheak_Click(object sender, RoutedEventArgs e)
        {
            BtnNoCrownCheak.Content = "완료!";
        }

        private void BtnAllPerpectCrownCheak_MouseEnter(object sender, MouseEventArgs e)
        {
            // 무지개 색상으로 배경 그라데이션 정의
            LinearGradientBrush rainbowBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };

            // GradientStop을 추가하여 무지개 색상 설정
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Red, 0));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0.17));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.33));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Green, 0.5));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Blue, 0.67));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Indigo, 0.83));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Violet, 1));

            // 버튼의 배경에 무지개 색상 적용
            BtnNoCrownCheak.Background = rainbowBrush;
            BtnNoCrownCheak.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
        }

        private void BtnAllPerpectCrownCheak_MouseLeave(object sender, MouseEventArgs e)
        {
            LinearGradientBrush rainbowBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };

            // GradientStop을 추가하여 무지개 색상 설정
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Salmon, 0));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Coral, 0.17));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Gold, 0.33));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.MediumSeaGreen, 0.5));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.DodgerBlue, 0.67));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.SlateBlue, 0.83));
            rainbowBrush.GradientStops.Add(new GradientStop(Colors.Plum, 1));

            // 버튼의 배경에 무지개 색상 적용
            BtnNoCrownCheak.Background = rainbowBrush;
        }
    }
}
