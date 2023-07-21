using System.Windows.Media.Imaging;

// 검색결과창으로 쓸 모델
namespace RandomTaikoSongProgram.Models
{
    internal class YoutubeItem
    {
        public string Title { get; set; } // 노래명
        public string Author { get; set; } // 작곡가명
        public string URL { get; set; } // 유튜브 링크주소
        public string ChannelTitle { get; set; } // 유튜브 채널명
        public BitmapImage Thumbnail { get; set; } // 썸네일
    }
}
