namespace MahjongPrototype.Domain
{
    public enum YakuKind
    {
        None = 0,

        // 1 han
        MenzenTsumo = 1,
        Reach = 2,
        Ippatsu = 3,
        Tanyao = 4,
        Pinfu = 5,
        Iipeikou = 6,
        YakuhaiSeatWind = 7,
        YakuhaiRoundWind = 8,
        YakuhaiWhiteDragon = 9,
        YakuhaiGreenDragon = 10,
        YakuhaiRedDragon = 11,
        RinshanKaihou = 12,
        Chankan = 13,
        HaiteiRaoyue = 14,
        HouteiRaoyui = 15,

        // 2 han
        DoubleReach = 100,
        SevenPairs = 101,
        Toitoi = 102,
        Sanankou = 103,
        Sankantsu = 104,
        SanshokuDoukou = 105,
        SanshokuDoujun = 106,
        Ittsuu = 107,
        Chanta = 108,
        Shousangen = 109,
        Honroutou = 110,

        // 3 han / higher normal yaku
        Junchan = 200,
        Ryanpeikou = 201,
        Honitsu = 202,
        Chinitsu = 203,

        // Yakuman
        KokushiMusou = 300,
        KokushiMusouThirteenWait = 301,
        Suuankou = 302,
        SuuankouTanki = 303,
        Daisangen = 304,
        Shousuushii = 305,
        Daisuushii = 306,
        Tsuuiisou = 307,
        Chinroutou = 308,
        Ryuuiisou = 309,
        ChuurenPoutou = 310,
        JunseiChuurenPoutou = 311,
        Suukantsu = 312,
        Tenhou = 313,
        Chiihou = 314,

        // Optional / rule-dependent
        Renhou = 400
    }
}
