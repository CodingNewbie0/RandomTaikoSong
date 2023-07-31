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
using System.Linq;
using System.Security.Policy;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace RandomTaikoSongProgram
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly Dictionary<Button, (Brush background, object content)> buttonStates;

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
        }

        bool isFavorite = false; // false -> openApi 검색해온결과, true -> 즐겨찾기 보기
        public MainWindow()
        {
            InitializeComponent();
            buttonStates = new Dictionary<Button, (Brush background, object content)>();
        }

        #region  < 세부 설정 버튼 >
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
        #endregion

        #region < 랜덤 추첨 >
        private void BtnRandomSong_Click(object sender, RoutedEventArgs e)
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
            if (GrdResult.SelectedItem != null)
            {
                MyData selectedData = (MyData)GrdResult.SelectedItem;
                songName = selectedData.Title;
            }

            var trailerWindow = new RandomWindow(songName);
            trailerWindow.Owner = this; // TrailerWindow의 부모는 MainWindow
            trailerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // 부모창의 정중앙에 위치
            trailerWindow.ShowDialog(); // 모달창
        }
        #endregion

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string connectionString = "server=210.119.12.53;user=root;database=taiko;password=12345;";
            TxtYouTubeName.Focus(); // 텍스트박스에 포커스 셋
            RunCrawling(connectionString);
        }

        #region < 데이터 그리드의 셀을 선택하면 맨위 텍스트박스에 정보가 들어감 >
        private void Title_Click(object sender, SelectionChangedEventArgs e)
        {
            if (GrdResult.SelectedItem != null)
            {
                // 선택한 항목에서 원하는 정보를 가져와서 TxtYouTubeName TextBox에 표시
                MyData selectedData = (MyData)GrdResult.SelectedItem;
                TxtYouTubeName.Text = selectedData.Title; // Title 정보
            }
        }
        #endregion

        #region < 크롤링 시작 >
        public void RunCrawling(string connectionString)
        {
            #region < 크롤링할 링크 목록 >
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
            #endregion

            #region < 데이터 그리드 원하는 값으로 변경 >
            GrdResult.Columns[0].Width = new DataGridLength(390);
            GrdResult.Columns[1].Width = new DataGridLength(440);
            GrdResult.Columns[2].Width = new DataGridLength(35);
            GrdResult.Columns[3].Width = new DataGridLength(35);
            GrdResult.Columns[4].Width = new DataGridLength(45);
            GrdResult.Columns[5].Width = new DataGridLength(35);
            GrdResult.Columns[6].Width = new DataGridLength(35);
            GrdResult.Columns[7].Width = new DataGridLength(110);
            GrdResult.RowHeight = 15;
            #endregion

            // 노래 정보를 크롤링
            void SongCrawlingAsync()
            {
                #region < 리스트 >
                _ = new List<Dictionary<string, object>>();
                List<string> titles = new List<string>();
                List<string> titleddak = new List<string>();
                List<string> artists = new List<string>();
                List<string> easyLists = new List<string>();
                List<string> normalLists = new List<string>();
                List<string> hardLists = new List<string>();
                List<string> oniLists = new List<string>();
                List<string> uraLists = new List<string>();
                List<string> genres = new List<string>();
                #endregion

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
                        #region < 노래명, 작곡가명, 장르 가져오기 (완성) > 
                        foreach (HtmlNode songNode in tableNodes)
                        {
                            int count = 0;

                            titleddak.Clear(); // titleddak 리스트를 비워서 초기화 ( 장르용 리스트 )
                            HtmlNodeCollection titleCells = songNode.SelectNodes(".//th"); // 곡명, 작곡가 붙어있는 HTML 태그
                            HtmlNodeCollection imgnodes = songNode.SelectNodes("//ul//li//a//img[@alt]"); // 장르명, 이미지 붙어있는 HTML 태그
                            if (titleCells != null)
                            {
                                #region < 작곡가명과 노래 >
                                foreach (HtmlNode cellNode in titleCells)
                                {
                                    var pTag = cellNode.SelectSingleNode(".//p"); // p태그는 작곡가 이름
                                    var spanTag = cellNode.SelectSingleNode(".//span"); // span태그는 동메달, 캇메달 곡 목록

                                    if (count >= 8) // 작곡가명 추가
                                    {
                                        if (pTag != null)
                                        {
                                            artists.Add(pTag.InnerText);
                                        }
                                    }
                                    pTag?.Remove(); // 작곡가명 삭제 (안그러면 제목에 작곡가명까지 같이 들어감)
                                    spanTag?.Remove(); // 메달 곡 목록 삭제
                                    string title = cellNode.InnerText.Trim();

                                    // 노래 제목 추가 (0번째 부터 7번째 요소는 제외하고 추가)
                                    if (count >= 8 && !string.IsNullOrEmpty(title))
                                    {
                                        titles.Add(title);
                                        titleddak.Add(title); // 위에서 리스트 클리어로 삭제하고 다시 다음 리스트를 받아옴
                                    }
                                    count++;
                                }
                                #endregion

                                #region < 장르명 >
                                switch (link)
                                {
                                    case "pops":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("ポップス");
                                        }
                                        break;
                                    case "kids":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("キッズ");
                                        }
                                        break;
                                    case "anime":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("アニメ");
                                        }
                                        break;
                                    case "vocaloid":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("ボーカロイド曲");
                                        }
                                        break;
                                    case "game":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("ゲームミュージック");
                                        }
                                        break;
                                    case "variety":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("バラエティ");
                                        }
                                        break;
                                    case "classic":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("クラシック");
                                        }
                                        break;
                                    case "namco":
                                        for (int i = 0; i < titleddak.Count; i++)
                                        {
                                            genres.Add("ナムコオリジナル");
                                        }
                                        break;
                                }
                                #endregion
                            }
                        }
                        #endregion

                        // 난이도 찾기 (쉬움 ~ 우라까지 하나씩 다 집어넣기) <진짜 찐 완성> 클린코딩 필요!!

                        #region < 간단 (완료) >
                        foreach (HtmlNode rowNode in tableNodes)
                        {
                            int[] easyTargetIndexes = new int[1500];
                            int easyStartNumber = 1;
                            int easyIncrement = 6;

                            // 만약 links 배열 변수가 "namco"에 도달했다면 easyIncrement 값을 7로 변경
                            if (link == "namco")
                            {
                                easyIncrement = 7;
                            }

                            for (int i = 0; i < easyTargetIndexes.Length; i++)
                            {
                                easyTargetIndexes[i] = easyStartNumber;
                                easyStartNumber += easyIncrement;
                            }

                            int count = 0;
                            List<int> repeatedIndexes = new List<int>(); // 반복된 인덱스를 저장하는 리스트
                            HtmlNodeCollection easyCells = rowNode.SelectNodes(".//td");
                            if (easyCells != null)
                            {
                                foreach (HtmlNode easyCellNode in easyCells)
                                {
                                    string easy = easyCellNode.InnerText.Trim();

                                    // targetIndexes 배열에 있는 인덱스만 easyLists에 추가합니다.
                                    if (easyTargetIndexes.Contains(count))
                                    {
                                        easyLists.Add(easy);
                                    }
                                    count++;
                                }
                            }
                        }
                        #endregion

                        #region < 보통 (완료) >
                        foreach (HtmlNode rowNode in tableNodes)
                        {
                            int[] normaltargetIndexes = new int[1500]; // 크기가 4인 배열 생성
                            int normalstartNumber = 2;
                            int normalincrement = 6;

                            // 만약 links 배열 변수가 "namco"에 도달했다면 easyIncrement 값을 7로 변경
                            if (link == "namco")
                            {
                                normalincrement = 7;
                            }

                            // 배열에 숫자 채우기
                            for (int i = 0; i < normaltargetIndexes.Length; i++)
                            {
                                normaltargetIndexes[i] = normalstartNumber;
                                normalstartNumber += normalincrement;
                            }

                            int count = 0;
                            List<int> repeatedIndexes = new List<int>(); // 반복된 인덱스를 저장하는 리스트
                            HtmlNodeCollection normalCells = rowNode.SelectNodes(".//td");
                            if (normalCells != null)
                            {
                                foreach (HtmlNode normalCellNode in normalCells)
                                {
                                    string normal = normalCellNode.InnerText.Trim();

                                    if (normaltargetIndexes.Contains(count))
                                    {
                                        normalLists.Add(normal);
                                    }
                                    count++;
                                }
                            }
                        }
                        #endregion

                        #region < 어려움 (완료) >
                        foreach (HtmlNode rowNode in tableNodes)
                        {
                            int[] hardtargetIndexes = new int[1500]; // 크기가 4인 배열 생성
                            int hardstartNumber = 3;
                            int hardincrement = 6;

                            // 만약 links 배열 변수가 "namco"에 도달했다면 easyIncrement 값을 7로 변경
                            if (link == "namco")
                            {
                                hardincrement = 7;
                            }

                            // 배열에 숫자 채우기
                            for (int i = 0; i < hardtargetIndexes.Length; i++)
                            {
                                hardtargetIndexes[i] = hardstartNumber;
                                hardstartNumber += hardincrement;
                            }

                            int count = 0;
                            List<int> repeatedIndexes = new List<int>(); // 반복된 인덱스를 저장하는 리스트
                            HtmlNodeCollection hardCells = rowNode.SelectNodes(".//td");
                            if (hardCells != null)
                            {
                                foreach (HtmlNode hardCellNode in hardCells)
                                {
                                    string hard = hardCellNode.InnerText.Trim();

                                    if (hardtargetIndexes.Contains(count))
                                    {
                                        hardLists.Add(hard);
                                    }
                                    count++;
                                }
                            }
                        }
                        #endregion

                        #region < 오니 (완료) >
                        foreach (HtmlNode rowNode in tableNodes)
                        {
                            int[] onitargetIndexes = new int[1500]; // 크기가 4인 배열 생성
                            int onistartNumber = 4;
                            int oniincrement = 6;

                            // 만약 links 배열 변수가 "namco"에 도달했다면 easyIncrement 값을 7로 변경
                            if (link == "namco")
                            {
                                oniincrement = 7;
                            }

                            // 배열에 숫자 채우기
                            for (int i = 0; i < onitargetIndexes.Length; i++)
                            {
                                onitargetIndexes[i] = onistartNumber;
                                onistartNumber += oniincrement;
                            }

                            int count = 0;
                            List<int> repeatedIndexes = new List<int>(); // 반복된 인덱스를 저장하는 리스트
                            HtmlNodeCollection oniCells = rowNode.SelectNodes(".//td");
                            if (oniCells != null)
                            {
                                foreach (HtmlNode oniCellNode in oniCells)
                                {
                                    string oni = oniCellNode.InnerText.Trim();

                                    if (onitargetIndexes.Contains(count))
                                    {
                                        oniLists.Add(oni);
                                    }
                                    count++;
                                }
                            }
                        }
                        #endregion

                        #region < 우라 (완료) >
                        foreach (HtmlNode rowNode in tableNodes)
                        {
                            int[] uratargetIndexes = new int[1500]; // 크기가 4인 배열 생성
                            int urastartNumber = 5;
                            int uraincrement = 6;

                            // 만약 links 배열 변수가 "namco"에 도달했다면 easyIncrement 값을 7로 변경
                            if (link == "namco")
                            {
                                uraincrement = 7;
                            }

                            // 배열에 숫자 채우기
                            for (int i = 0; i < uratargetIndexes.Length; i++)
                            {
                                uratargetIndexes[i] = urastartNumber;
                                urastartNumber += uraincrement;
                            }

                            int count = 0;
                            List<int> repeatedIndexes = new List<int>(); // 반복된 인덱스를 저장하는 리스트
                            HtmlNodeCollection uraCells = rowNode.SelectNodes(".//td");
                            if (uraCells != null)
                            {
                                foreach (HtmlNode uraCellNode in uraCells)
                                {
                                    string ura = uraCellNode.InnerText.Trim();

                                    // targetIndexes 배열에 있는 인덱스만 easyLists에 추가합니다.
                                    if (uratargetIndexes.Contains(count))
                                    {
                                        uraLists.Add(ura);
                                    }
                                    count++;
                                }
                            }
                        }
                        #endregion

                        #region < DataGrid에 데이터를 추가할 리스트 생성 >
                        if (link == links.Last())
                        {
                            for (int i = 0; i < titles.Count; i++)
                            {
                                MyData data = new MyData
                                {
                                    Title = titles[i],
                                    Composer = artists[i],
                                    Easy = easyLists[i],
                                    Normal = normalLists[i],
                                    Hard = hardLists[i],
                                    Oni = oniLists[i],
                                    Ura = uraLists[i],
                                    Genre = genres[i]
                                };
                                dataList.Add(data);
                            }
                        }
                        #endregion
                    }
                }                
                GrdResult.ItemsSource = dataList; // 데이터 그리드에 정보를 넣음
            }
        }
        #endregion

        #region < 찾은거 유튜브로 보기 (완료) >
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
            if (GrdResult.SelectedItem != null)
            {
                MyData selectedData = (MyData)GrdResult.SelectedItem;
                songName = selectedData.Title; 
            }

            var trailerWindow = new TrailerWindow(songName);
            trailerWindow.Owner = this; // TrailerWindow의 부모는 MainWindow
            trailerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // 부모창의 정중앙에 위치
            trailerWindow.ShowDialog(); // 모달창

        }
        #endregion

        #region < 즐겨찾기 목록 이벤트 핸들러 (완료) >

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

                    var query = @"INSERT INTO songs (Title, Composer, Easy, Normal, Hard, Oni, Ura, Genre, release) 
                                  VALUES (@Title, @Composer, @Easy, @Normal, @Hard, @Oni, @Ura, @Genre, @release);";


                    var insRes = 0;                    
                    foreach (YoutubeSongItem item in list)
                    {
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Title", item.Title);
                        cmd.Parameters.AddWithValue("@Composer", item.Composer);
                        cmd.Parameters.AddWithValue("@Easy", item.Easy);
                        cmd.Parameters.AddWithValue("@Normal", item.Normal);
                        cmd.Parameters.AddWithValue("@Hard", item.Hard);
                        cmd.Parameters.AddWithValue("@Oni", item.Oni);
                        cmd.Parameters.AddWithValue("@Ura", item.Ura);
                        cmd.Parameters.AddWithValue("@Genre", item.Genre);

                        insRes += cmd.ExecuteNonQuery();
                    }
                    
                    if (list.Count == insRes)
                    {
                        await Commons.ShowMessageAsync("저장", "즐겨찾기성공");
                    }
                    else
                    {
                        await Commons.ShowMessageAsync("저장", "즐겨찾기오류 관리자에게 문의하세요.");
                    }
                }
            }
            catch (Exception ex)
            {
                await Commons.ShowMessageAsync("오류", $"즐겨찾기 오류{ex.Message}");
            }            
        }
        #endregion

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
                        cmd.Parameters.AddWithValue("@Title", item.Title);
                        cmd.Parameters.AddWithValue("@Composer", item.Composer);
                        cmd.Parameters.AddWithValue("@Easy", item.Easy);
                        cmd.Parameters.AddWithValue("@Normal", item.Normal);
                        cmd.Parameters.AddWithValue("@Hard", item.Hard);
                        cmd.Parameters.AddWithValue("@Oni", item.Oni);
                        cmd.Parameters.AddWithValue("@Ura", item.Ura);
                        cmd.Parameters.AddWithValue("@Genre", item.Genre);
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
