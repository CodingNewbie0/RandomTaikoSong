using System.Windows.Media; // 검색기능으로 쓸거

namespace RandomTaikoSongProgram.Models
{
    internal class YSI
    {
        public int Id { get; set; }
        public string Title { get; set; } // 노래제목
        public string Composer { get; set; } // 작곡가
        public string Easy { get; set; } // 쉬움
        public string Normal { get; set; } // 보통
        public string Hard { get; set; } // 어려움
        public string Oni { get; set; } // 오니
        public string Ura { get; set; } // 우라
        public string Genre { get; set; } // 장르
        public ImageSource NoCrown { get; set; } // 왕관 이미지를 저장할 ImageSource 속성
        public ImageSource ClearCrown { get; set; }
        public ImageSource FullComboCrown { get; set; }
        public ImageSource AllPerpectCrown { get; set; }
    }
}
