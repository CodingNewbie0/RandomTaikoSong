using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using RandomTaikoSongProgram.Logics;
using RandomTaikoSongProgram.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using HtmlAgilityPack;
using ControlzEx.Standard;
using Org.BouncyCastle.Utilities;
using System.Windows.Data;

namespace RandomTaikoSongProgram
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private Dictionary<Button, (Brush background, object content)> buttonStates;

        public class MyData
        {
            public string Title { get; set; }
            public string Composer { get; set; }
            public string Easy { get; set; }
            public string Normal { get; set; }
            public string Hard { get; set; }
            public string Oni { get; set; }
            public string Ura { get; set; }
            public string Genre { get; set; }
            public string ReleaseDate { get; set; }

        }

        bool isFavorite = false; // false -> openApi 검색해온결과, true -> 즐겨찾기 보기
        public MainWindow()
        {
            InitializeComponent();
            buttonStates = new Dictionary<Button, (Brush background, object content)>();
        }

        // 세부 설정 버튼
        private void BtnOffset_Click(object sender, RoutedEventArgs e) 
        {
            try
            {
                // MySQL 연결 문자열 설정
                string connectionString = "server=210.119.12.53;user=root;database=taiko;password=12345;";

                // MySQL 연결 생성
                MySqlConnection connection = new MySqlConnection(connectionString);

                // 연결 열기
                connection.Open();

                // SQL 쿼리 작성
                string query = "SELECT * FROM songs";

                // 쿼리 실행할 커맨드 객체 생성
                MySqlCommand command = new MySqlCommand(query, connection);

                // 데이터를 담을 DataTable 생성
                DataTable dataTable = new DataTable();

                // 쿼리 실행하고 데이터를 DataTable에 로드
                MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                dataAdapter.Fill(dataTable);

                GrdResult.ItemsSource = dataTable.DefaultView;

                // 연결 닫기
                connection.Close();

                // DataTable 사용하여 데이터 처리
                // 예시로 콘솔에 모든 데이터를 출력합니다.
                                             

                foreach (DataRow row in dataTable.Rows)
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        Console.Write(row[column] + " ");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                // 오류 처리
                MessageBox.Show("오류가 발생했습니다: " + ex.Message);
            }
        }

        // 랜덤 추첨
        private void BtnRandomSong_Click(object sender, RoutedEventArgs e) 
        {

        }

        // 검색버튼, OpenAPI 노래 검색 (완료)
        private async void BtnSearchYouTube_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtYouTubeName.Text))
            {
                await Commons.ShowMessageAsync("검색", "검색할 노래명을 입력하세요.");
                return;
            }

            try
            {
                SearchYouTube(TxtYouTubeName.Text);
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("오류", $"오류 발생 : {ex.Message}");
            }
        }

        // 엔터로 검색창 넘어가게 이벤트 넣기
        private void TxtYouTubeName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSearchYouTube_Click(sender, e);
            }
        }

        // 실제 검색메서드
        private async void SearchYouTube(string SongName)
        {
            string encoding_songName = HttpUtility.UrlEncode(SongName, Encoding.UTF8); // 노래 검색 URL
            string result = string.Empty; // 결과값

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
                    Release = Convert.ToDateTime(val["Release"])
                };
                songItems.Add(songItem);
            }

            this.DataContext = songItems;
            isFavorite = false; // 즐겨찾기가 아님.
            StsResult.Content = $"OpenAPI 조회 {songItems.Count} 건 조회 완료";
        }
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string connectionString = "server=210.119.12.53;user=root;database=taiko;password=12345;";
            TxtYouTubeName.Focus(); // 텍스트박스에 포커스 셋
            RunCrowling(connectionString);
        }


        public void RunCrowling(string connectionString)
        {
            // 크롤링할 링크 목록
            string[] links = {
            "pops",
            "kids",
            "anime",
            "vocaloid",
            "game",
            "variety",
            "classic",
            "namco"
            };
            SongCrawlingAsync();

            // 노래 정보를 크롤링
            void SongCrawlingAsync()
            {
                // 리스트에 배열 저장
                List<Dictionary<string, object>> taikoInfoList = new List<Dictionary<string, object>>();
                List<string> titles = new List<string>();
                List<string> artists = new List<string>();
                //List<List<string>> difficultyLists = new List<List<string>>();
                List<string> difficultyLists = new List<string>();
                string[] stringArray;
                List<string> genres = new List<string>();

                int linkNum = 0;
                // 데이터를 담을 List 생성
                List<MyData> dataList = new List<MyData>();
                foreach (string link in links)
                {                                     
                    // 미리 선언한 내용을 링크와 결합
                    string url = $"https://taiko.namco-ch.net/taiko/songlist/{link}.php";

                    // URL에서 HTML 코드 가져오기
                    WebClient webClient = new WebClient
                    {
                        Encoding = Encoding.GetEncoding("utf-8")
                    };
                    string htmlCode = webClient.DownloadString(url);

                    // 일본어 문자열을 utf-8로 인코딩(묵음<ㅡ>은 ?로 표시되는 오류있음)
                    byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(htmlCode);
                    string utf8String = Encoding.UTF8.GetString(bytes);

                    // HtmlDocument 객체 생성 및 UTF-8로 인코딩된 HTML 코드 로드
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(utf8String);

                    // 노래 리스트 테이블을 찾음
                    HtmlNodeCollection tableNodes = doc.DocumentNode.SelectNodes("//table");

                    if (tableNodes != null)
                    {
                        // 곡명 가져오기 (완성)
                        int count = 0;
                        foreach (HtmlNode songNode in tableNodes)
                        {
                            HtmlNodeCollection titleCells = songNode.SelectNodes(".//th");
                            if (titleCells != null)
                            {
                                foreach (HtmlNode cellNode in titleCells)
                                {
                                    var pTag = cellNode.SelectSingleNode(".//p");
                                    pTag?.Remove();
                                    string title = cellNode.InnerText.Trim();

                                    // 0번째 부터 7번째 요소는 제외하고 추가
                                    if (count >= 8)
                                    {
                                        titles.Add(title);
                                    }
                                    count++;
                                }
                            }
                        }

                        // 작곡가명
                        foreach (HtmlNode artistNode in tableNodes)
                        {
                            HtmlNodeCollection artistCells = artistNode.SelectNodes(".//th//p");
                            string artist = "";
                            if (artistCells != null)
                            {
                                if (artistCells.Count > 1)
                                {
                                    artist = artistCells[0].InnerText.Trim();
                                    foreach (HtmlNode cellNode in artistCells)
                                    {
                                        Console.Write(cellNode.InnerText + " ");
                                    }
                                }
                            }
                            artists.Add(artist);
                        }

                        // 난이도 찾기
                        foreach (HtmlNode rowNode in tableNodes)
                        {
                            HtmlNodeCollection difficultyCells = rowNode.SelectNodes(".//td");
                            var difficultyList = new List<string>();

                            string data = "";
                            int n = 0;

                            if (difficultyCells != null)
                            {

                                foreach (HtmlNode cellNode in difficultyCells)
                                {
                                    difficultyList.Add(cellNode.InnerText.Trim());
                                    Console.Write(cellNode.InnerText + " ");
                                    data += cellNode.InnerText;
                                    n++;
                                    if (n >= 5) { data += "///"; }
                                }

                                // 문자열 data를 "///" 기준으로 나누어서 difficultyLists 리스트에 추가
                                difficultyLists.AddRange(data.Split(new string[] { "///" }, StringSplitOptions.None));

                                Console.WriteLine("으아아앙ㄴ말ㄴ머리ㅏㄴ머리ㅏㄴ머리ㅏㄴㅁㅁ++++++++++++-----------");
                            }
                            // 레벨별 난이도
                            /*
                            if (difficultyList.Count == 6)
                            {
                                string[] difficultyLevels = { "쉬움", "보통", "어려움", "오니", "우라" };
                                difficultyList = new List<string>();
                                for (int j = 1; j < 6; j++)
                                {
                                    difficultyList.Add($"{difficultyLevels[j - 1]} : {difficultyList[j]}");
                                }
                            }
                            */
                            //difficultyLists.Add(difficultyList);
                        }

                        // 장르 찾기 (완성)
                        // <img> 태그들 선택
                        HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img");
                        if (imgNodes != null)
                        {
                            int n = 0;
                            foreach (HtmlNode imgNode in imgNodes)
                            {
                                // <img> 태그의 alt 속성 확인
                                string genre = imgNode.GetAttributeValue("alt", "");
                                if (n >= 2) genres.Add(genre);
                                n++;
                                if (n >= 10) break;
                            }
                        }


                        // DataGrid에 데이터를 추가할 리스트 생성
                        List<Dictionary<string, object>> dataGridItems = new List<Dictionary<string, object>>();




                        for (int i = 0; i < titles.Count; i++)
                        {
                            // 데이터 생성과 추가

                            Console.WriteLine(titles.Count);
                            Console.WriteLine(difficultyLists.Count);
                            MyData data1 = new MyData { Title = titles[i], Composer = "", Easy = "", Normal = "", Hard = "", Oni = "", Ura = difficultyLists[i], Genre = genres[linkNum], ReleaseDate = "" };

                            dataList.Add(data1);

                        }
                        linkNum++;

                        GrdResult.RowHeight = 10; // 높이 값은 원하는 값으로 변경 가능

                        // 데이터 그리드에 데이터 설정

                    }
                }


                GrdResult.ItemsSource = dataList;

                for (int i = 0; i < titles.Count; i++)
                {



                    //GrdResult.Items.Add(new { Title1 = titles[i], Composer1 = artists[i], Difficulty1 = difficultyLists[i], Genre1 = genres[i] });
                    // 노래정보를 리스트에 추가
                    /*
                    var taikoInfo = new Dictionary<string, object>
                    {
                        { "Title", titles[i] },
                        { "Composer", artists[i] },
                        { "Difficulty", difficultyLists[i] },
                        { "Genre", genres[i] }
                    };
                    taikoInfoList.Add(taikoInfo);
                    */
                }
                //GrdResult.ItemsSource = taikoInfoList;
            }
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

            var trailerWindow = new TrailerWindow(songName)
            {
                Owner = this, // TrailerWindow의 부모는 MainWindow
                WindowStartupLocation = WindowStartupLocation.CenterOwner // 부모창의 정중앙에 위치
            };
            trailerWindow.ShowDialog(); // 모달창

        }

        #region <즐겨찾기 목록 이벤트 핸들러>

        private async void BtnAddFavorite_Click(object sender, RoutedEventArgs e) // 즐겨찾기 추가
        {
            List<YoutubeSongItem> list = new List<YoutubeSongItem>();
            //if (GrdResult.SelectedItems.Count == 0)
            //{
            //    await Commons.ShowMessageAsync("오류", "즐겨찾기에 추가할 노래를 선택하세요(복수선택 가능)");
            //    return;
            //}

            //if (isFavorite)
            //{
            //    await Commons.ShowMessageAsync("오류", "이미 즐겨찾기한 노래입니다.");
            //    return;
            //}

            #region < MySQL 연결 >
            try
            {
                // DB 연결확인
                using (MySqlConnection conn = new MySqlConnection(Commons.myConnString))
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    var query = @"INSERT INTO songs (title, composer, difficulty, genre, release) VALUES (@title, @composer, @difficulty, @genre, @release);";


                    var insRes = 0;                    
                    foreach (YoutubeSongItem item in list)
                    {
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@title", item.Title);
                        cmd.Parameters.AddWithValue("@composer", item.Composer);
                        cmd.Parameters.AddWithValue("@difficulty", item.Difficulty);
                        cmd.Parameters.AddWithValue("@genre", item.Genre);
                        cmd.Parameters.AddWithValue("@release", item.Release);

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

                    var query = @"SELECT * FROM songs;";

                    var insRes = 0;
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    foreach (YoutubeSongItem item in list)
                    {
                        cmd.Parameters.AddWithValue("@Id", item.Id);
                        cmd.Parameters.AddWithValue("@Title", item.Title);
                        cmd.Parameters.AddWithValue("@Composer", item.Composer);
                        cmd.Parameters.AddWithValue("@Difficulty", item.Difficulty);
                        cmd.Parameters.AddWithValue("@Genre", item.Genre);
                        cmd.Parameters.AddWithValue("@Release", DateTime.Now);
                    }

                    this.DataContext = list;
                    isFavorite = true;
                    StsResult.Content = $"즐겨찾기 {list.Count} 건 조회완료";
                }
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("오류", $"DB조회 오류\n{ex.Message}");
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
            try 
            {
                using (MySqlConnection conn = new MySqlConnection(Commons.myConnString))
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
#endregion

        // ----------------------- 왕관 체크 이벤트 핸들러 -------------------------       

        #region <미클리어 이벤트 핸들러>
        private void BtnNoCrownCheak_MouseLeave(object sender, MouseEventArgs e) // 마우스가 버튼을 떠날 때의 동작
        {
            // 버튼의 배경색을 원래대로 복구
            BtnNoCrownCheak.Background = Brushes.Brown;
            Button button = (Button)sender;

            // 원래 배경색과 컨텐츠로 복원합니다.
            if (buttonStates.TryGetValue(button, out var state))
            {
                button.Content = state.content;
                button.Background = state.background;
            }
        }

        private void BtnNoCrownCheak_MouseEnter(object sender, MouseEventArgs e) // 마우스가 버튼에 들어갔을 때의 동작
        {
            Button button = (Button)sender;
            // 원래 배경색과 컨텐츠를 저장합니다.
            if (!buttonStates.ContainsKey(button))
                buttonStates.Add(button, (button.Background, button.Content));

            // 버튼의 배경색을 연한 갈색(브라운)으로 변경.
            BtnNoCrownCheak.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
            BtnNoCrownCheak.Background = Brushes.Tan;
        }
        private async void BtnNoCrownCheak_Click(object sender, RoutedEventArgs e) // 버튼이 클릭되었을 때의 동작
        {
            if (GrdResult.SelectedItems.Count == 0)
            {
                await Commons.ShowMessageAsync("선택오류", "노래를 선택하세요.");
                return;
            }

            BtnNoCrownCheak.Content = "완료!";
        }

        #endregion

        #region <클리어 이벤트 핸들러>
        private async void BtnClearCrownCheak_Click(object sender, RoutedEventArgs e)
        {
            if (GrdResult.SelectedItems.Count == 0)
            {
                await Commons.ShowMessageAsync("선택오류", "노래를 선택하세요.");
                return;
            }

            BtnClearCrownCheak.Content = "완료!";
        }

        private void BtnClearCrownCheak_MouseEnter(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            // 원래 배경색과 컨텐츠를 저장합니다.
            if (!buttonStates.ContainsKey(button))
                buttonStates.Add(button, (button.Background, button.Content));

            // 버튼의 배경색을 연한 회색으로 변경.
            BtnClearCrownCheak.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
            BtnClearCrownCheak.Background = Brushes.LightGray;
        }

        private void BtnClearCrownCheak_MouseLeave(object sender, MouseEventArgs e)
        {
            BtnClearCrownCheak.Background = Brushes.Gray;
            Button button = (Button)sender;

            // 원래 배경색과 컨텐츠로 복원합니다.
            if (buttonStates.TryGetValue(button, out var state))
            {
                button.Content = state.content;
                button.Background = state.background;
            }
        }
        #endregion

        #region <풀콤보 이벤트 핸들러>
        private async void BtnFullComboCrownCheak_Click(object sender, RoutedEventArgs e)
        {
            if (GrdResult.SelectedItems.Count == 0)
            {
                await Commons.ShowMessageAsync("선택오류", "노래를 선택하세요.");
                return;
            }

            BtnFullComboCrownCheak.Content = "완료!";
        }

        private void BtnFullComboCrownCheak_MouseEnter(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            // 원래 배경색과 컨텐츠를 저장합니다.
            if (!buttonStates.ContainsKey(button))
                buttonStates.Add(button, (button.Background, button.Content));

            // 버튼의 배경색을 빛나는 노란색으로 변경하고, 컨텐츠를 아이콘으로 설정합니다.
            button.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
            button.Background = Brushes.LightGoldenrodYellow;
        }

        private void BtnFullComboCrownCheak_MouseLeave(object sender, MouseEventArgs e)
        {
            BtnFullComboCrownCheak.Background = Brushes.Gold;
            Button button = (Button)sender;

            // 원래 배경색과 컨텐츠로 복원합니다.
            if (buttonStates.TryGetValue(button, out var state))
            {
                button.Content = state.content;
                button.Background = state.background;
            }
        }
        #endregion

        #region <퍼펙트 이벤트 핸들러>
        private async void BtnAllPerpectCrownCheak_Click(object sender, RoutedEventArgs e)
        {
            if (GrdResult.SelectedItems.Count == 0)
            {
                await Commons.ShowMessageAsync("선택오류", "노래를 선택하세요.");
                return;
            }

            BtnAllPerpectCrownCheak.Content = "완료!";
        }

        private void BtnAllPerpectCrownCheak_MouseEnter(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            // 원래 배경색과 컨텐츠를 저장합니다.
            if (!buttonStates.ContainsKey(button))
                buttonStates.Add(button, (button.Background, button.Content));

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
            BtnAllPerpectCrownCheak.Background = rainbowBrush;
            BtnAllPerpectCrownCheak.Content = new PackIconJamIcons { Kind = PackIconJamIconsKind.Crown, Margin = new Thickness(5) };
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
            BtnAllPerpectCrownCheak.Background = rainbowBrush;
            Button button = (Button)sender;

            // 원래 배경색과 컨텐츠로 복원합니다.
            if (buttonStates.TryGetValue(button, out var state))
            {
                button.Content = state.content;
                button.Background = state.background;
            }
        }
        #endregion
    }
}
