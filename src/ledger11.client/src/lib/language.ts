export const tesseractLanguages = {
    "afr": "Afrikaans", "amh": "Amharic", "ara": "Arabic", "asm": "Assamese", "aze": "Azerbaijani",
    "aze_cyrl": "Azerbaijani - Cyrillic", "bel": "Belarusian", "ben": "Bengali", "bod": "Tibetan",
    "bos": "Bosnian", "bre": "Breton", "bul": "Bulgarian", "cat": "Catalan; Valencian", "ceb": "Cebuano",
    "ces": "Czech", "chi_sim": "Chinese - Simplified", "chi_tra": "Chinese - Traditional",
    "chr": "Cherokee", "cos": "Corsican", "cym": "Welsh", "dan": "Danish", "deu": "German",
    "dzo": "Dzongkha", "ell": "Greek, Modern (1453-)", "eng": "English",
    "est": "Estonian", "eus": "Basque", "fao": "Faroese", "fas": "Persian",
    "fil": "Filipino", "fin": "Finnish", "fra": "French",
    "fry": "Western Frisian", "gla": "Scottish Gaelic", "gle": "Irish", "glg": "Galician",
    "guj": "Gujarati", "hat": "Haitian; Haitian Creole", "heb": "Hebrew", "hin": "Hindi", "hrv": "Croatian",
    "hun": "Hungarian", "hye": "Armenian", "iku": "Inuktitut", "ind": "Indonesian", "isl": "Icelandic",
    "ita": "Italian", "jav": "Javanese", "jpn": "Japanese", "kan": "Kannada",
    "kat": "Georgian", "kaz": "Kazakh", "khm": "Central Khmer", "kir": "Kirghiz; Kyrgyz",
    "kmr": "Kurmanji (Kurdish - Latin Script)", "kor": "Korean", "kor_vert": "Korean (vertical)", "lao": "Lao",
    "lav": "Latvian", "lit": "Lithuanian", "ltz": "Luxembourgish", "mal": "Malayalam", "mar": "Marathi",
    "mkd": "Macedonian", "mlt": "Maltese", "mon": "Mongolian", "mri": "Maori", "msa": "Malay",
    "mya": "Burmese", "nep": "Nepali", "nld": "Dutch; Flemish", "nor": "Norwegian",
    "ori": "Oriya", "pan": "Panjabi; Punjabi", "pol": "Polish",
    "por": "Portuguese", "pus": "Pushto; Pashto", "que": "Quechua", "ron": "Romanian; Moldavian; Moldovan",
    "rus": "Russian", "sin": "Sinhala; Sinhalese", "slk": "Slovak", "slv": "Slovenian",
    "snd": "Sindhi", "spa": "Spanish; Castilian", "sqi": "Albanian",
    "srp": "Serbian", "srp_latn": "Serbian - Latin", "sun": "Sundanese", "swa": "Swahili", "swe": "Swedish",
    "tam": "Tamil", "tat": "Tatar", "tel": "Telugu", "tgk": "Tajik", "tha": "Thai",
    "tir": "Tigrinya", "tur": "Turkish", "uig": "Uighur; Uyghur", "ukr": "Ukrainian",
    "urd": "Urdu", "uzb": "Uzbek", "uzb_cyrl": "Uzbek - Cyrillic", "vie": "Vietnamese", "yid": "Yiddish",
    "yor": "Yoruba"
};

export const languageList = Object.entries(tesseractLanguages)
    .map(([code, name]) => ({ code, name }))
    .sort((a, b) => a.name.localeCompare(b.name));
