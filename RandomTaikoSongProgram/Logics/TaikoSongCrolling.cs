using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;

class Program // 프로그램 시작
{
    static async Task Main(string[] args) // 비동기 메인 메서드로 변경
    {
        await TaikoSongCrolling.RunProgram(args); // 비동기로 RunProgram 메서드 실행
    }
}

class TaikoSongCrolling
{
    // DB 연결
    public static async Task RunProgram(string[] args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }
        // MySQL 연결 정보 설정
        string server = "210.119.12.53"; // MySQL 서버 주소
        string database = "Taiko"; // 데이터베이스 이름
        string uid = "root"; // 사용자 이름
        string password = "12345"; // 비밀번호
        string connectionString = $"Server={server};Database={database};Uid={uid};Pwd={password};";

        // RunCrowling 메서드를 비동기로 실행
        RunCrowling(connectionString).Wait();
    }

    // 크롤링 시작
    static async Task RunCrowling(string connectionString)
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

        // Timer 생성 및 설정
        Timer timer = new Timer
        {
            Interval = TimeSpan.FromHours(24).TotalMilliseconds // 24시간 (1일) 간격으로 실행
        };
        timer.Elapsed += async (sender, e) => await OnTimerElapsedAsync(connectionString, links); // 비동기 이벤트 핸들러로 변경
        timer.Start();

        // 프로그램 실행 유지 (24시간마다 크롤링 작업이 실행됩니다)
        Console.WriteLine("크롤링 작업이 매일 00:00에 실행됩니다. 종료하려면 'q'를 입력하세요.");


        // 노래 정보를 DB에 저장
        async Task OnTimerElapsedAsync(string connString, string[] songLinks)
        {
            foreach (string link in links)
            {
                string url = $"https://taiko.namco-ch.net/taiko/songlist/{link}.php";
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 결과를 저장할 목록
                List<Dictionary<string, object>> taikoInfoList = new List<Dictionary<string, object>>();

                // 곡 리스트 테이블을 찾음
                var songTable = doc.DocumentNode.SelectSingleNode("//table");

                if (songTable != null)
                {
                    // 테이블에서 행 찾음
                    var rows = songTable.SelectNodes(".//tr");

                    for (int i = 2; i < rows.Count; i++)
                    {
                        var row = rows[i];

                        // 작곡가 찾기
                        var artistCells = row.SelectNodes(".//p");
                        string artist = "";
                        if (artistCells.Count > 1)
                        {
                            artist = artistCells[0].InnerText.Trim();
                        }
                        else
                        {
                            artist = artistCells[0]?.InnerText.Trim() ?? "";
                        }

                        // 노래 제목 찾기
                        var titleCells = row.SelectNodes(".//th");
                        string title = "";
                        if (titleCells != null)
                        {
                            foreach (var cell in titleCells)
                            {
                                var pTag = cell.SelectSingleNode(".//p");
                                pTag?.Remove();
                            }

                            title = titleCells.Count > 1 ? titleCells[0].InnerText.Trim() : titleCells[0]?.InnerText.Trim() ?? "";
                        }

                        // 난이도 찾기
                        var difficultyCells = row.SelectNodes(".//td");
                        var difficultyList = new List<string>();
                        if (difficultyCells != null)
                        {
                            foreach (var cell in difficultyCells)
                            {
                                difficultyList.Add(cell.InnerText.Trim());
                            }
                        }

                        // 세부레벨 및 난이도 정렬(파파마마 서포트는 제외)
                        if (difficultyList.Count == 6)
                        {
                            string[] difficultyLevels = { "쉬움", "보통", "어려움", "오니", "우라" };
                            difficultyList = new List<string>();
                            for (int j = 1; j < 6; j++)
                            {
                                difficultyList.Add($"{difficultyLevels[j - 1]} : {difficultyList[j]}");
                            }
                        }

                        // 목록에 곡 정보 추가
                        var taikoInfo = new Dictionary<string, object>
                    {
                        { "Song Title", title },
                        { "Composer", artist },
                        { "Difficulty", difficultyList }
                    };
                        taikoInfoList.Add(taikoInfo);
                    }
                }

                // 노래 정보를 MySQL 데이터베이스에 저장
                using (MySqlConnection connection = new MySqlConnection(connString))
                {
                    try
                    {
                        await connection.OpenAsync(); // 비동기로 연결을 열기

                        foreach (var taikoInfo in taikoInfoList)
                        {
                            string title = taikoInfo["Song Title"].ToString();
                            string composer = taikoInfo["Composer"].ToString();
                            string difficulty = string.Join(", ", (List<string>)taikoInfo["Difficulty"]);

                            string insertQuery = $"INSERT INTO songs (title, composer, difficulty) VALUES (@title, @composer, @difficulty)";
                            using (MySqlCommand cmd = new MySqlCommand(insertQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@title", title);
                                cmd.Parameters.AddWithValue("@composer", composer);
                                cmd.Parameters.AddWithValue("@difficulty", difficulty);
                                await cmd.ExecuteNonQueryAsync(); // 비동기로 쿼리 실행
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }

                Console.WriteLine("곡 정보가 정상적으로 MySQL 서버에 저장되었습니다.");
            }
        }
    }
}
