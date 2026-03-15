namespace hi_site_ideas_blazor.Models;

public class Giveaway
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Boss { get; set; } = "";
    public string Player { get; set; } = "";
    public string Item { get; set; } = "";
    public GiveawayStatus Status { get; set; } = GiveawayStatus.Active;
    public int StartKc { get; set; }
    public int CurrentKc { get; set; }
    public int? DropKc { get; set; }
    public List<Prize> Prizes { get; set; } = [];
    public List<Guess> Guesses { get; set; } = [];

    public string ImageUrl => string.IsNullOrEmpty(Item)
        ? ""
        : $"https://oldschool.runescape.wiki/images/{Uri.EscapeDataString(Item.Replace(' ', '_'))}.png";

    public Giveaway Clone() => new()
    {
        Id = Id,
        Title = Title,
        Boss = Boss,
        Player = Player,
        Item = Item,
        Status = Status,
        StartKc = StartKc,
        CurrentKc = CurrentKc,
        DropKc = DropKc,
        Prizes = Prizes.Select(p => new Prize { Label = p.Label, Type = p.Type, Reward = p.Reward, Count = p.Count }).ToList(),
        Guesses = Guesses.Select(g => new Guess { Name = g.Name, GuessKc = g.GuessKc }).ToList(),
    };
}

public enum GiveawayStatus { Active, Dropped, Completed }

public class Prize
{
    public string Label { get; set; } = "";
    public string Type { get; set; } = "closest";
    public string Reward { get; set; } = "";
    public int? Count { get; set; }
}

public class Guess
{
    public string Name { get; set; } = "";
    public int GuessKc { get; set; }
}

public record BossInfo(string Name, bool Sync, string[] Items);

public static class GiveawayConstants
{
    public static readonly string[] Members =
    [
        "0 xf","0xFFFFFF","1 Tear","1mbuedHeart","2 R","2 tick DlCK","26ll98","5DY2",
        "A Bing User","A Lucky Mfer","A Morman","Abdily","Acro in osrs","Ahrim Jobber",
        "Also Zodiac","Alven","Angry Asura","Anuvahoodie","Anynamefree","AOXQ",
        "apexofirons","Aphrodarty","Astromiles","Auggie Iron","Ax06","AyYoDaddyo",
        "BattousaiBTW","Bazinga","Be R","Bears Hard","Beef Hot Dog","Beer OL Dad",
        "Bengis Khan","Benocidal","Beskar Hilt","Beta Indo","big nutsac","BigKnobb",
        "BlackBeards","Blaziken X","BlG Zaddy","Blocked","Boo Duh","BoraPora","Brouim",
        "Btony","Buenosdiaz11","BuiltMeBoat","Ca5h3w","carpcatcher","Castalidin",
        "cba to slay","CertIronBoy","Chaillia","ChilliDaPero","ChilliDeNero",
        "Choco salad","Chode Lock","Chu mak","Chudzee","Chummy Asura","Cin card",
        "cloudsofiron","Cois","CrazyBinge","Crushed Guam","D4RE","Daddy lron","DAIJ0UBU",
        "Damned Blitz","DaneCatoVT","DarkBow","De L3ns","Dee Eye Why","Died I Guess",
        "Dilzn","dipsht iron","dispo addict","Doctor Kun","DoomBx","Dope Dream",
        "Dr Mcsexy","Dragon Car","DreamsOfIron","E r n i e","E85Only","Een Duvelken",
        "EEPR0M","egos2","El Papicito","EscapeMaIron","Et tu Civis","EU Sanctions",
        "ExElementz3","F A l Z","Fake Dylan","False Bard","FamousJameis","Fe Anthracis",
        "Fe Jiggles","Fe l o n","Fe Peon","Fe Snodwater","FeHikikomori","Femboymaxing",
        "Femes","Ferrum Santa","Finbezz","Fire9x","flendygim","Flume NZ","forceskins",
        "Forgot MlLK","FossilDunged","fragile gimp","Franklyy","Freddy4200","Freepop",
        "Freepopp","Fridgie","Fullironboss","G r I M s by","G uilherme","Game Ovaries",
        "Garchomp Z","Gearbeach","Geeving","Geevs Uncle","Geography","GI LOG",
        "GIM Clayman","GIM Goblino","GIM Khujo","GIM Reckless","GIM Sparkman",
        "GIM Wake Who","GIM WubbaBub","GIM0derca","GimJarm","Gimme Gim Me","GIMP Aloha",
        "Gimp Jim FE","glockmad","gm lad","Goat No Cap","Gong Kong","Goo fe","Got 2 Nut",
        "Grandma G","Grant M","Griggle4Game","Grout","h a m i i","H0bo P0w3r","Hairy Hog",
        "HarderDeek","Hardie","Hardly Iron","Hemalurgic","HeresALlama","hey im sam","HjP",
        "hm08","hockey slut","honsonn","hot dog 62","House Vitur","HumbleDonkey","hyenau",
        "I REPENTED","I3lackI3eard","Ibicenco","Ice Beir","iFencig","IFiveBoro","Igoshyox",
        "IIIizzou","IM A Spoon","im Dez","IM Dooug","Im Virta","IM Walrusmon","IMHaukkumies",
        "imPikes","iPaul07","IR0N H0B0","Ir0n Zilm","IrnChad","Iron Actuary","Iron Alabama",
        "Iron Bournos","Iron Dad 10","Iron Dankie","Iron Design","Iron Dog Btw",
        "Iron E Toast","Iron Ferno","Iron Gavius","Iron Gizmos","Iron Joker1","Iron LT Man",
        "Iron Mund","Iron Murdaro","Iron muttu","Iron Nephi","Iron Peens","Iron puma jr",
        "Iron Seejay","Iron SilentV","Iron Sooners","iron ssgoku","Iron Tri3","iron v p",
        "Iron Wealth","Iron Willbo","IronChefOzzy","ironcoyote25","IronDadGamer",
        "ironman turk","IronMarf","IronNyrax","IronPwnCat","IronRiolu","IronSly",
        "IronSnotter","IronWr4th","iSolo","iSporde","ITIuttman","Its Newt","Its Saboo",
        "Its Swag","Its Wilmo","itty","J d x o","J sax","Jackolantern","Jautismo",
        "Jessssy","Jesssy","Jrux","Jsg55","Ju no lucky","JWizzle45","K7zza","KBDs Nuts",
        "KeefyGuam","Keijo Kaasu","KephrisLabia","Khqled","Kinda Fuzzy","King Astros",
        "kingsawyer1","KnobbHead","KraayFish","Krepsi","LandyBruce","le fe weasel",
        "LeanpocketsX","Learnertob","Lelc","LeThuanJames","Lex 26","limptwiglet",
        "Logging","Lonely","Long Sword","LookasX","Lord Rodram","Lost Golem","lTlizzou",
        "LTN14","Luwuker","M a c a iii","M79","Maagequit","madmyler","Mancer","Max Goof",
        "Mc I n n e s","me kebab","Medvedd","Melon y","Meowsosaurus","Merczor",
        "MinnesDuluth","MisfitGrinds","MobileHutch","Moendio","Moo Cat Mooo","Moss Jam",
        "MousseNukkl","Mtgdan m8","Nam a","Nama","Nandii","Nanoh Jr","Nans Reeboks",
        "Ncik","NerdFling","Nerdo","nerfete","Next Jesus","NezVro","No Free Time",
        "No Papa Why","No Saint","Noo Trading","NosferatuMan","Not Justin","Not OG enuf",
        "Nut Dragger","O KawaiiKoto","Ol GreyHelm","Omav","omegadubs","omegavision",
        "Orcinc","Osrs Play3r","Ouvia","OvergrownPea","P ete Y","Pale Hermit","Papa Like",
        "Pawnyada","Peenie bee","pezzleton","Pig Bearman","Pikachu","pikkujukkaa",
        "Pirate Smee","pizzalord321","Platypus","Play w Self","Play w Toes","pointed spot",
        "Prpleux","Pure Actuary","Purrdaddyuwu","Purrmommyuwu","Qaqnlefany","Qthegreat",
        "Quailz","R0KER","rabid buniez","RanarrSweets","Rapid HC","Rapidd","rawgliz",
        "Real Scapin","redmarmalade","Rett","Riding SoLow","Rigotoni","RockyBalbofa",
        "Rogue nr2","Rogue Soap","Rolig","Ron Bombadil","RonCoomer","Rotund Tuber",
        "RoxIRM","RoxOSRS","rpar btw","Rug Ryder","Rui z","Rwlz","S truggles",
        "SaintMarty","SaintZero","SaltyDeHond","samisdecent","sat iv","satiw","Scapin",
        "Schackz","schevecorner","Scoopy18","Scoopy18 GIM","Sexiro","Sexyro",
        "Shadezblood","Shakey Box","Sheindre","SHENANlGATOR","Ship Wheel","shippey15",
        "Shovelnate","Sixers Chip","Sixers Curse","Sizen","SkelIy","Skrenk","Slingy",
        "Smee Junior","Smoking Duty","SmurfPapi","SnatsGap","Snorlax Y","Snowlif3",
        "Solenoids","Solo Androx","Solo Russian","SpidersWeb","SSF","static836",
        "Steam Vents","stonysnail","Storij","Sunday Beers","SXV3N","Table Plus",
        "Taboo Tim","Takis","Tatoes","TekThuan","Terrorthepic","The Iron SOS","TheBabyYoda",
        "thegreylife","Thema","Third ages","Thuan Frank","Tides","Tile Wilder","Tim Horton",
        "Timmys","To Be Frank","Tokedaddy","TokHurt Kal","Tom Astro","Toxic V",
        "Toxic Virgin","Trictagon","TriHard NerD","Try RS","TzTok Jem","TzTokJizzSok",
        "Uhai0123","UIM Valdarok","uimweasel","Unnarsson","Vaporion","Velkoja",
        "Ventureskatr","Vial of Fate","Vial of Tism","Victonburg","Viet Gong","w3swift",
        "Wacachi","Waffle Horse","Weims","Wet P","WhoLikesIron","Willlbo","Winkybinky",
        "WisdomStat","Woodbrain","Wool Scarf","Xantara","xBeerdo","XgodofironX","xGundam",
        "y e llo w","Yahoo Magoo","Yeka","Yeqzz","Yotnar","Young Texbi","YOUTHANASlA",
        "Z and","za3emkosa","ZanezDeNonce","ZilataniumPK",
    ];

    public static readonly BossInfo[] Bosses =
    [
        new("Alchemical Hydra", true, ["Hydra's claw", "Hydra tail", "Hydra leather", "Hydra's eye", "Hydra's fang", "Hydra's heart", "Jar of chemicals", "Alchemical hydra heads"]),
        new("Cerberus", true, ["Primordial crystal", "Pegasian crystal", "Eternal crystal", "Smouldering stone", "Jar of souls", "Hellpuppy"]),
        new("Chambers of Xeric", true, ["Twisted bow", "Elder maul", "Kodai insignia", "Dragon claws", "Ancestral hat", "Ancestral robe top", "Ancestral robe bottom", "Dexterous prayer scroll", "Arcane prayer scroll", "Twisted buckler", "Dinh's bulwark", "Dragon hunter crossbow", "Olmlet"]),
        new("Commander Zilyana", true, ["Saradomin sword", "Saradomin's light", "Armadyl crossbow", "Saradomin hilt", "Pet zilyana"]),
        new("Corrupted Gauntlet", true, ["Enhanced crystal weapon seed", "Crystal armour seed", "Youngllef"]),
        new("Corporeal Beast", true, ["Spectral sigil", "Arcane sigil", "Elysian sigil", "Holy elixir", "Spirit shield", "Pet dark core", "Jar of spirits"]),
        new("General Graardor", true, ["Bandos chestplate", "Bandos tassets", "Bandos boots", "Bandos hilt", "Pet general graardor"]),
        new("Giant Mole", true, ["Baby mole"]),
        new("Grotesque Guardians", true, ["Granite maul", "Black tourmaline core", "Jar of stone", "Noon"]),
        new("K'ril Tsutsaroth", true, ["Staff of the dead", "Zamorakian spear", "Steam battlestaff", "Zamorak hilt", "Pet k'ril tsutsaroth"]),
        new("Kalphite Queen", true, ["Kalphite princess", "Jar of sand", "Dragon chainbody", "Dragon 2h sword"]),
        new("King Black Dragon", true, ["Prince black dragon", "Dragon pickaxe", "Draconic visage", "KBD heads"]),
        new("Kraken", true, ["Kraken tentacle", "Trident of the seas (full)", "Jar of dirt", "Pet kraken"]),
        new("Kree'arra", true, ["Armadyl helmet", "Armadyl chestplate", "Armadyl chainskirt", "Armadyl hilt", "Pet kree'arra"]),
        new("Nightmare", true, ["Nightmare staff", "Inquisitor's great helm", "Inquisitor's hauberk", "Inquisitor's plateskirt", "Inquisitor's mace", "Eldritch orb", "Harmonised orb", "Volatile orb", "Jar of dreams", "Little nightmare"]),
        new("Nex", true, ["Zaryte vambraces", "Nihil horn", "Torva full helm", "Torva platebody", "Torva platelegs", "Ancient hilt", "Nexling"]),
        new("Phantom Muspah", true, ["Ancient essence", "Venator shard", "Muphin"]),
        new("Sarachnis", true, ["Sarachnis cudgel", "Jar of eyes", "Sraracha"]),
        new("Scorpia", true, ["Scorpia's offspring"]),
        new("Skotizo", true, ["Dark claw", "Jar of darkness", "Skotos"]),
        new("Tempoross", true, ["Tiny tempor", "Dragon harpoon", "Tome of water"]),
        new("The Gauntlet", true, ["Crystal armour seed", "Crystal weapon seed", "Youngllef"]),
        new("Theatre of Blood", true, ["Scythe of vitur", "Ghrazi rapier", "Sanguinesti staff", "Justiciar faceguard", "Justiciar chestguard", "Justiciar legguards", "Avernic defender hilt", "Lil' zik"]),
        new("Thermonuclear Smoke Devil", true, ["Occult necklace", "Smoke battlestaff", "Dragon chainbody", "Jar of smoke", "Pet smoke devil"]),
        new("Tombs of Amascut", true, ["Tumeken's shadow", "Elidinis' ward", "Masori mask", "Masori body", "Masori chaps", "Lightbearer", "Osmumten's fang", "Tumeken's guardian"]),
        new("Vorkath", true, ["Vorki", "Skeletal visage", "Draconic visage", "Dragonbone necklace", "Jar of decay"]),
        new("Zulrah", true, ["Tanzanite fang", "Magic fang", "Serpentine visage", "Uncut onyx", "Tanzanite mutagen", "Magma mutagen", "Jar of swamp", "Pet snakeling"]),
    ];

    public static readonly (string Value, string Label)[] PrizeTypes =
    [
        ("exact", "Exact KC"),
        ("closest", "Closest guess"),
        ("2nd", "2nd closest"),
        ("3rd", "3rd closest"),
        ("random", "Random"),
    ];

    public static readonly (string Value, string Label)[] StatusOptions =
    [
        ("Active", "In Progress"),
        ("Dropped", "Drop Received"),
        ("Completed", "Completed"),
    ];

    public static List<Guess> PickMembers(int seed, int count, int rangeMin, int rangeMax)
    {
        var rng = new SeededRandom(seed);
        var shuffled = Members.OrderBy(_ => rng.Next()).Take(count).ToList();
        return shuffled.Select(name => new Guess
        {
            Name = name,
            GuessKc = (int)Math.Round(rangeMin + rng.NextDouble() * (rangeMax - rangeMin)),
        }).ToList();
    }

    public static List<Giveaway> GetInitialGiveaways() =>
    [
        new()
        {
            Id = "cg-seed",
            Title = "Enhanced Crystal Weapon Seed",
            Boss = "Corrupted Gauntlet",
            Player = "Lex 26",
            Item = "Enhanced crystal weapon seed",
            Status = GiveawayStatus.Active,
            StartKc = 125,
            CurrentKc = 1321,
            DropKc = null,
            Prizes =
            [
                new() { Label = "Closest guess", Type = "closest", Reward = "2 Bonds" },
                new() { Label = "Random", Type = "random", Reward = "1 Bond", Count = 2 },
            ],
            Guesses = PickMembers(99, 22, 130, 1057),
        },
        new()
        {
            Id = "zulrah",
            Title = "Zulrah Pet",
            Boss = "Zulrah",
            Player = "Lex 26",
            Item = "Pet snakeling",
            Status = GiveawayStatus.Dropped,
            StartKc = 1411,
            CurrentKc = 1587,
            DropKc = 1587,
            Prizes =
            [
                new() { Label = "Closest guess", Type = "closest", Reward = "5M GP" },
                new() { Label = "Random", Type = "random", Reward = "2M GP", Count = 2 },
            ],
            Guesses = PickMembers(42, 24, 1415, 3000),
        },
        new()
        {
            Id = "hydra-claw",
            Title = "Hydra Claw",
            Boss = "Alchemical Hydra",
            Player = "Omav",
            Item = "Hydra's claw",
            Status = GiveawayStatus.Completed,
            StartKc = 3287,
            CurrentKc = 4467,
            DropKc = 4467,
            Prizes =
            [
                new() { Label = "Exact KC", Type = "exact", Reward = "5 Bonds" },
                new() { Label = "Closest guess", Type = "closest", Reward = "2 Bonds" },
                new() { Label = "2nd closest", Type = "2nd", Reward = "1 Bond" },
            ],
            Guesses = PickMembers(77, 26, 3290, 5500),
        },
    ];
}

public class SeededRandom
{
    private int _seed;

    public SeededRandom(int seed) => _seed = seed;

    public double NextDouble()
    {
        _seed = (int)(((long)_seed * 16807) % 2147483647);
        return _seed / 2147483647.0;
    }

    public int Next() => (int)(NextDouble() * int.MaxValue);
}
