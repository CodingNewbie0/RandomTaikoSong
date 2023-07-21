using System;
using System.Windows.Media;

namespace RandomTaikoSongProgram.Models
{
    internal class YSI
    {
        public string Title { get; set; } // 노래제목
        public string Composer { get; set; } // 작곡가
        public string Difficulty { get; set; } // 난이도
        public string Genre { get; set; } // 장르
        public string Youtube { get; set; } // 유튜브링크
        public DateTime Release { get; set; } // 업데이트시간
        public ImageSource NoCrown { get; set; } // 왕관 이미지를 저장할 ImageSource 속성
        public ImageSource ClearCrown { get; set; }
        public ImageSource FullComboCrown { get; set; }
        public ImageSource AllPerpectCrown { get; set; }
    }
}
