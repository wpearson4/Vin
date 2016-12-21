﻿#region license
// Vin
// .NET Library for Validating Vehicle Identification Numbers
// Copyright 2016 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace DaleNewman {

    public static class Vin {

        private static readonly object LockObject = new object();

        private const int ValidVinLength = 17;
        private const int CheckDigitIndex = 8;
        // Character weights for 17 characters in VIN
        private static readonly int[] CharacterWeights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };
        private static readonly int DefaultYear = (DateTime.Now.Year / 30) * 30;
        private static readonly int NextYear = DateTime.Now.Year + 1;

        private static readonly Dictionary<char, int> Years = new Dictionary<char, int> {
            { 'A', 0 },
            { 'B', 1 },
            { 'C', 2 },
            { 'D', 3 },
            { 'E', 4 },
            { 'F', 5 },
            { 'G', 6 },
            { 'H', 7 },
            { 'J', 8 },
            { 'K', 9 },
            { 'L', 10 },
            { 'M', 11 },
            { 'N', 12 },
            { 'P', 13 },
            { 'R', 14 },
            { 'S', 15 },
            { 'T', 16 },
            { 'V', 17 },
            { 'W', 18 },
            { 'X', 19 },
            { 'Y', 20 },
            { '1', 21 },
            { '2', 22 },
            { '3', 23 },
            { '4', 24 },
            { '5', 25 },
            { '6', 26 },
            { '7', 27 },
            { '8', 28 },
            { '9', 29 }
        };

        private static readonly Dictionary<char, int> ValidCheckCharacters = new Dictionary<char, int> {
            {'0',0},
            {'1',1},
            {'2',2},
            {'3',3},
            {'4',4},
            {'5',5},
            {'6',6},
            {'7',7},
            {'8',8},
            {'9',9},
            {'X',10}
        };

        // Character transliteration, how to get from characters to numbers
        private static readonly Dictionary<char, int> CharacterTransliteration = new Dictionary<char, int> {
            {'A', 1},
            {'B', 2},
            {'C', 3},
            {'D', 4},
            {'E', 5},
            {'F', 6},
            {'G', 7},
            {'H', 8},
            {'J', 1},
            {'K', 2},
            {'L', 3},
            {'M', 4},
            {'N', 5},
            {'P', 7},
            {'R', 9},
            {'S', 2},
            {'T', 3},
            {'U', 4},
            {'V', 5},
            {'W', 6},
            {'X', 7},
            {'Y', 8},
            {'Z', 9},
            {'1', 1},
            {'2', 2},
            {'3', 3},
            {'4', 4},
            {'5', 5},
            {'6', 6},
            {'7', 7},
            {'8', 8},
            {'9', 9},
            {'0', 0}
        };

        // lock and load wmi if GetWorldManufacturer is used
        private static Dictionary<string, string> _wmi;
        private static Dictionary<string, string> WorldManufacturerIdentifiers
        {
            get
            {
                if (_wmi == null) {
                    lock (LockObject) {
                        _wmi = new Dictionary<string, string> {
                        {"AAV", "Volkswagen South Africa"},
                        {"AC5", "Hyundai South Africa"},
                        {"ADD", "Hyundai South Africa"},
                        {"AFA", "Ford South Africa"},
                        {"AHT", "Toyota South Africa"},
                        {"JA3", "Mitsubishi"},
                        {"JA4", "Mitsubishi"},
                        {"JA", "Isuzu"},
                        {"JD", "Daihatsu"},
                        {"JF", "Fuji Heavy Industries (Subaru)"},
                        {"JH", "Honda"},
                        {"JK", "Kawasaki (motorcycles)"},
                        {"JL5", "Mitsubishi Fuso"},
                        {"JMB", "Mitsubishi Motors"},
                        {"JMY", "Mitsubishi Motors"},
                        {"JMZ", "Mazda"},
                        {"JN", "Nissan"},
                        {"JS", "Suzuki"},
                        {"JT", "Toyota"},
                        {"JY", "Yamaha (motorcycles)"},
                        {"KL", "Daewoo General Motors South Korea"},
                        {"KM", "Hyundai"},
                        {"KMY", "Daelim (motorcycles)"},
                        {"KM1", "Hyosung (motorcycles)"},
                        {"KN", "Kia"},
                        {"KNM", "Renault Samsung"},
                        {"KPA", "SsangYong"},
                        {"KPT", "SsangYong"},
                        {"LAN", "Changzhou Yamasaki Motorcycle"},
                        {"LBB", "Zhejiang Qianjiang Motorcycle (Keeway/Generic)"},
                        {"LBE", "Beijing Hyundai"},
                        {"LBM", "Zongshen Piaggio"},
                        {"LBP", "Chongqing Jainshe Yamaha (motorcycles)"},
                        {"LB2", "Geely Motorcycles"},
                        {"LCE", "Hangzhou Chunfeng Motorcycles (CFMOTO)"},
                        {"LDC", "Dong Feng Peugeot Citroen (DPCA), China"},
                        {"LDD", "Dandong Huanghai Automobile"},
                        {"LDN", "SouEast Motor"},
                        {"LDY", "Zhongtong Coach, China"},
                        {"LET", "Jiangling-Isuzu Motors, China"},
                        {"LE4", "Beijing Benz, China"},
                        {"LFB", "FAW, China (busses)"},
                        {"LFG", "Taizhou Chuanl Motorcycle Manufacturing"},
                        {"LFP", "FAW, China (passenger vehicles)"},
                        {"LFT", "FAW, China (trailers)"},
                        {"LFV", "FAW-Volkswagen, China"},
                        {"LFW", "FAW JieFang, China"},
                        {"LFY", "Changshu Light Motorcycle Factory"},
                        {"LGB", "Dong Feng (DFM), China"},
                        {"LGH", "Qoros (formerly Dong Feng (DFM)), China"},
                        {"LGX", "BYD Auto, China"},
                        {"LHB", "Beijing Automotive Industry Holding"},
                        {"LH1", "FAW-Haima, China"},
                        {"LJC", "JAC, China"},
                        {"LJ1", "JAC, China"},
                        {"LKL", "Suzhou King Long, China"},
                        {"LL6", "Hunan Changfeng Manufacture Joint-Stock"},
                        {"LL8", "Linhai (ATV)"},
                        {"LMC", "Suzuki Hong Kong (motorcycles)"},
                        {"LPR", "Yamaha Hong Kong (motorcycles)"},
                        {"LSG", "Shanghai General Motors, China"},
                        {"LSJ", "MG Motor UK Limited - SAIC Motor, Shanghai, China"},
                        {"LSV", "Shanghai Volkswagen, China"},
                        {"LSY", "Brilliance Zhonghua"},
                        {"LTV", "Toyota Tian Jin"},
                        {"LUC", "Guangqi Honda, China"},
                        {"LVS", "Ford Chang An"},
                        {"LVV", "Chery, China"},
                        {"LVZ", "Dong Feng Sokon Motor Company (DFSK)"},
                        {"LZM", "MAN China"},
                        {"LZE", "Isuzu Guangzhou, China"},
                        {"LZG", "Shaanxi Automobile Group, China"},
                        {"LZP", "Zhongshan Guochi Motorcycle (Baotian)"},
                        {"LZY", "Yutong Zhengzhou, China"},
                        {"LZZ", "Chongqing Shuangzing Mech & Elec (Howo)"},
                        {"L4B", "Xingyue Group (motorcycles)"},
                        {"L5C", "KangDi (ATV)"},
                        {"L5K", "Zhejiang Yongkang Easy Vehicle"},
                        {"L5N", "Zhejiang Taotao, China (ATV & motorcycles)"},
                        {"L5Y", "Merato Motorcycle Taizhou Zhongneng"},
                        {"L85", "Zhejiang Yongkang Huabao Electric Appliance"},
                        {"L8X", "Zhejiang Summit Huawin Motorcycle"},
                        {"MAB", "Mahindra & Mahindra"},
                        {"MAC", "Mahindra & Mahindra"},
                        {"MAJ", "Ford India"},
                        {"MAK", "Honda Siel Cars India"},
                        {"MAL", "Hyundai"},
                        {"MAT", "Tata Motors"},
                        {"MA1", "Mahindra & Mahindra"},
                        {"MA3", "Suzuki India (Maruti)"},
                        {"MA6", "GM India"},
                        {"MA7", "Mitsubishi India (formerly Honda)"},
                        {"MBH", "Suzuki India (Maruti)"},
                        {"MBJ", "Toyota India"},
                        {"MBR", "Mercedes-Benz India"},
                        {"MB1", "Ashok Leyland"},
                        {"MCA", "Fiat India"},
                        {"MCB", "GM India"},
                        {"MC2", "Volvo Eicher commercial vehicles limited."},
                        {"MDH", "Nissan India"},
                        {"MD2", "Bajaj Auto"},
                        {"MEE", "Renault India"},
                        {"MEX", "Volkswagen India"},
                        {"MHF", "Toyota Indonesia"},
                        {"MHR", "Honda Indonesia"},
                        {"MLC", "Suzuki Thailand"},
                        {"MLH", "Honda Thailand"},
                        {"MMB", "Mitsubishi Thailand"},
                        {"MMC", "Mitsubishi Thailand"},
                        {"MMM", "Chevrolet Thailand"},
                        {"MMT", "Mitsubishi Thailand"},
                        {"MM8", "Mazda Thailand"},
                        {"MNB", "Ford Thailand"},
                        {"MNT", "Nissan Thailand"},
                        {"MPA", "Isuzu Thailand"},
                        {"MP1", "Isuzu Thailand"},
                        {"MRH", "Honda Thailand"},
                        {"MR0", "Toyota Thailand"},
                        {"NLA", "Honda Türkiye"},
                        {"NLE", "Mercedes-Benz Türk Truck"},
                        {"NLH", "Hyundai Assan"},
                        {"NM0", "Ford Turkey"},
                        {"NM4", "Tofaş Türk"},
                        {"NMT", "Toyota Türkiye"},
                        {"PE1", "Ford Phillipines"},
                        {"PE3", "Mazda Phillipines"},
                        {"PL1", "Proton, Malaysia"},
                        {"PNA", "NAZA, Malaysia (Peugeot)"},
                        {"RFB", "Kymco, Taiwan"},
                        {"RFG", "Sanyang SYM, Taiwan"},
                        {"RFL", "Adly, Taiwan"},
                        {"RFT", "CPI, Taiwan"},
                        {"RF3", "Aeon Motor, Taiwan"},
                        {"SAL", "Land Rover"},
                        {"SAJ", "Jaguar"},
                        {"SAR", "Rover"},
                        {"SB1", "Toyota UK"},
                        {"SBM", "McLaren"},
                        {"SCA", "Rolls Royce"},
                        {"SCB", "Bentley"},
                        {"SCC", "Lotus Cars"},
                        {"SCE", "DeLorean Motor Cars N. Ireland (UK)"},
                        {"SCF", "Aston"},
                        {"SDB", "Peugeot UK (formerly Talbot)"},
                        {"SED", "General Motors Luton Plant"},
                        {"SEY", "LDV"},
                        {"SFA", "Ford UK"},
                        {"SFD", "Alexander Dennis UK"},
                        {"SHH", "Honda UK"},
                        {"SHS", "Honda UK"},
                        {"SJN", "Nissan UK"},
                        {"SKF", "Vauxhall"},
                        {"SLP", "JCB Research UK"},
                        {"SMT", "Triumph Motorcycles"},
                        {"SUF", "Fiat Auto Poland"},
                        {"SUL", "FSC (Poland)"},
                        {"SUP", "FSO-Daewoo (Poland)"},
                        {"SUU", "Solaris Bus & Coach (Poland)"},
                        {"TCC", "Micro Compact Car AG (smart 1998-1999)"},
                        {"TDM", "QUANTYA Swiss Electric Movement (Switzerland)"},
                        {"TMA", "Hyundai Motor Manufacturing Czech"},
                        {"TMB", "Škoda (Czech Republic)"},
                        {"TMK", "Karosa (Czech Republic)"},
                        {"TMP", "Škoda trolleybuses (Czech Republic)"},
                        {"TMT", "Tatra (Czech Republic)"},
                        {"TM9", "Škoda trolleybuses (Czech Republic)"},
                        {"TNE", "TAZ"},
                        {"TN9", "Karosa (Czech Republic)"},
                        {"TRA", "Ikarus Bus"},
                        {"TRU", "Audi Hungary"},
                        {"TSE", "Ikarus Egyedi Autobuszgyar, (Hungary)"},
                        {"TSM", "Suzuki Hungary"},
                        {"TW1", "Toyota Caetano Portugal"},
                        {"TYA", "Mitsubishi Trucks Portugal"},
                        {"TYB", "Mitsubishi Trucks Portugal"},
                        {"UU1", "Renault Dacia, (Romania)"},
                        {"UU3", "ARO"},
                        {"UU6", "Daewoo Romania"},
                        {"U5Y", "Kia Motors Slovakia"},
                        {"U6Y", "Kia Motors Slovakia"},
                        {"VAG", "Magna Steyr Puch"},
                        {"VAN", "MAN Austria"},
                        {"VBK", "KTM (Motorcycles)"},
                        {"VF1", "Renault"},
                        {"VF2", "Renault"},
                        {"VF3", "Peugeot"},
                        {"VF4", "Talbot"},
                        {"VF6", "Renault (Trucks & Buses)"},
                        {"VF7", "Citroën"},
                        {"VF8", "Matra"},
                        {"VG5", "MBK (motorcycles)"},
                        {"VLU", "Scania France"},
                        {"VN1", "SOVAB (France)"},
                        {"VNE", "Irisbus (France)"},
                        {"VNK", "Toyota France"},
                        {"VNV", "Renault-Nissan"},
                        {"VSA", "Mercedes-Benz Spain"},
                        {"VSE", "Suzuki Spain (Santana Motors)"},
                        {"VSK", "Nissan Spain"},
                        {"VSS", "SEAT"},
                        {"VSX", "Opel Spain"},
                        {"VS6", "Ford Spain"},
                        {"VS7", "Citroën Spain"},
                        {"VS9", "Carrocerias Ayats (Spain)"},
                        {"VTH", "Derbi (motorcycles)"},
                        {"VTT", "Suzuki Spain (motorcycles)"},
                        {"VV9", "TAURO Spain"},
                        {"VWA", "Nissan Spain"},
                        {"VWV", "Volkswagen Spain"},
                        {"VX1", "Zastava / Yugo Serbia"},
                        {"WAG", "Neoplan"},
                        {"WAU", "Audi"},
                        {"WA1", "Audi SUV"},
                        {"WBA", "BMW"},
                        {"WBS", "BMW M"},
                        {"WDA", "Daimler"},
                        {"WDB", "Mercedes-Benz"},
                        {"WDC", "DaimlerChrysler"},
                        {"WDD", "Mercedes-Benz"},
                        {"WDF", "Mercedes-Benz (commercial vehicles)"},
                        {"WEB", "Evobus GmbH (Mercedes-Bus)"},
                        {"WJM", "Iveco Magirus"},
                        {"WF0", "Ford Germany"},
                        {"WMA", "MAN Germany"},
                        {"WME", "smart"},
                        {"WMW", "MINI"},
                        {"WMX", "Mercedes-AMG"},
                        {"WP0", "Porsche"},
                        {"WP1", "Porsche SUV"},
                        {"W0L", "Opel"},
                        {"WUA", "quattro GmbH"},
                        {"WVG", "Volkswagen MPV/SUV"},
                        {"WVW", "Volkswagen"},
                        {"WV1", "Volkswagen Commercial Vehicles"},
                        {"WV2", "Volkswagen Bus/Van"},
                        {"WV3", "Volkswagen Trucks"},
                        {"XLB", "Volvo (NedCar)"},
                        {"XLE", "Scania Netherlands"},
                        {"XLR", "DAF (trucks)"},
                        {"XMC", "Mitsubishi (NedCar)"},
                        {"XTA", "Lada/AutoVaz (Russia)"},
                        {"XTT", "UAZ/Sollers (Russia)"},
                        {"XUF", "General Motors Russia"},
                        {"XUU", "AvtoTor (Russia, General Motors SKD)"},
                        {"XW8", "Volkswagen Group Russia"},
                        {"XWB", "UZ-Daewoo (Uzbekistan)"},
                        {"XWE", "AvtoTor (Russia, Hyundai-Kia SKD)"},
                        {"X4X", "AvtoTor (Russia, BMW SKD)"},
                        {"X7L", "Renault AvtoFramos (Russia)"},
                        {"X7M", "Hyundai TagAZ (Russia)"},
                        {"YBW", "Volkswagen Belgium"},
                        {"YCM", "Mazda Belgium"},
                        {"YE2", "Van Hool (buses)"},
                        {"YK1", "Saab-Valmet Finland"},
                        {"YS2", "Scania AB"},
                        {"YS3", "Saab"},
                        {"YS4", "Scania Bus"},
                        {"YTN", "Saab NEVS"},
                        {"YU7", "Husaberg (motorcycles)"},
                        {"YV1", "Volvo Cars"},
                        {"YV4", "Volvo Cars"},
                        {"YV2", "Volvo Trucks"},
                        {"YV3", "Volvo Buses"},
                        {"Y6D", "Zaporozhets/AvtoZAZ (Ukraine)"},
                        {"ZAA", "Autobianchi"},
                        {"ZAM", "Maserati"},
                        {"ZAP", "Piaggio/Vespa/Gilera"},
                        {"ZAR", "Alfa Romeo"},
                        {"ZBN", "Benelli"},
                        {"ZCG", "Cagiva SpA / MV Agusta"},
                        {"ZCF", "Iveco"},
                        {"ZDM", "Ducati Motor Holdings SpA"},
                        {"ZDF", "Ferrari Dino"},
                        {"ZD0", "Yamaha Italy"},
                        {"ZD3", "Beta Motor"},
                        {"ZD4", "Aprilia"},
                        {"ZFA", "Fiat"},
                        {"ZFC", "Fiat V.I."},
                        {"ZFF", "Ferrari"},
                        {"ZGU", "Moto Guzzi"},
                        {"ZHW", "Lamborghini"},
                        {"ZJM", "Malaguti"},
                        {"ZJN", "Innocenti"},
                        {"ZKH", "Husqvarna Motorcycles Italy"},
                        {"ZLA", "Lancia"},
                        {"ZOM", "OM"},
                        {"Z8M", "Marussia (Russia)"},
                        {"1B3", "Dodge"},
                        {"1C3", "Chrysler"},
                        {"1C6", "Chrysler"},
                        {"1D3", "Dodge"},
                        {"1FA", "Ford Motor Company"},
                        {"1FB", "Ford Motor Company"},
                        {"1FC", "Ford Motor Company"},
                        {"1FD", "Ford Motor Company"},
                        {"1FM", "Ford Motor Company"},
                        {"1FT", "Ford Motor Company"},
                        {"1FU", "Freightliner"},
                        {"1FV", "Freightliner"},
                        {"1F9", "FWD Corp."},
                        {"1G", "General Motors USA"},
                        {"1GC", "Chevrolet Truck USA"},
                        {"1GT", "GMC Truck USA"},
                        {"1G1", "Chevrolet USA"},
                        {"1G2", "Pontiac USA"},
                        {"1G3", "Oldsmobile USA"},
                        {"1G4", "Buick USA"},
                        {"1G6", "Cadillac USA"},
                        {"1G8", "Saturn USA"},
                        {"1GM", "Pontiac USA"},
                        {"1GY", "Cadillac USA"},
                        {"1H", "Honda USA"},
                        {"1HD", "Harley-Davidson"},
                        {"1J4", "Jeep"},
                        {"1L", "Lincoln USA"},
                        {"1ME", "Mercury USA"},
                        {"1M1", "Mack Truck USA"},
                        {"1M2", "Mack Truck USA"},
                        {"1M3", "Mack Truck USA"},
                        {"1M4", "Mack Truck USA"},
                        {"1M9", "Mynatt Truck & Equipment"},
                        {"1N", "Nissan USA"},
                        {"1NX", "NUMMI USA"},
                        {"1P3", "Plymouth USA"},
                        {"1R9", "Roadrunner Hay Squeeze USA"},
                        {"1VW", "Volkswagen USA"},
                        {"1XK", "Kenworth USA"},
                        {"1XP", "Peterbilt USA"},
                        {"1YV", "Mazda USA (AutoAlliance International)"},
                        {"1ZV", "Ford (AutoAlliance International)"},
                        {"2A4", "Chrysler Canada"},
                        {"2B3", "Dodge Canada"},
                        {"2B7", "Dodge Canada"},
                        {"2C3", "Chrysler Canada"},
                        {"2CN", "CAMI"},
                        {"2D3", "Dodge Canada"},
                        {"2FA", "Ford Motor Company Canada"},
                        {"2FB", "Ford Motor Company Canada"},
                        {"2FC", "Ford Motor Company Canada"},
                        {"2FM", "Ford Motor Company Canada"},
                        {"2FT", "Ford Motor Company Canada"},
                        {"2FU", "Freightliner"},
                        {"2FV", "Freightliner"},
                        {"2FZ", "Sterling"},
                        {"2G", "General Motors Canada"},
                        {"2G1", "Chevrolet Canada"},
                        {"2G2", "Pontiac Canada"},
                        {"2G3", "Oldsmobile Canada"},
                        {"2G4", "Buick Canada"},
                        {"2HG", "Honda Canada"},
                        {"2HK", "Honda Canada"},
                        {"2HJ", "Honda Canada"},
                        {"2HM", "Hyundai Canada"},
                        {"2M", "Mercury"},
                        {"2NV", "Nova Bus Canada"},
                        {"2P3", "Plymouth Canada"},
                        {"2T", "Toyota Canada"},
                        {"2V4", "Volkswagen Canada"},
                        {"2V8", "Volkswagen Canada"},
                        {"2WK", "Western Star"},
                        {"2WL", "Western Star"},
                        {"2WM", "Western Star"},
                        {"3C4", "Chrysler Mexico"},
                        {"3D3", "Dodge Mexico"},
                        {"3FA", "Ford Motor Company Mexico"},
                        {"3FE", "Ford Motor Company Mexico"},
                        {"3G", "General Motors Mexico"},
                        {"3H", "Honda Mexico"},
                        {"3JB", "BRP Mexico (all-terrain vehicles)"},
                        {"3MZ", "Mazda Mexico"},
                        {"3N", "Nissan Mexico"},
                        {"3P3", "Plymouth Mexico"},
                        {"3VW", "Volkswagen Mexico"},
                        {"4F", "Mazda USA"},
                        {"4JG", "Mercedes-Benz USA"},
                        {"4M", "Mercury"},
                        {"4RK", "Nova Bus USA"},
                        {"4S", "Subaru-Isuzu Automotive"},
                        {"4T", "Toyota"},
                        {"4T9", "Lumen Motors (zero-emission mid-engined car)"},
                        {"4UF", "Arctic Cat Inc."},
                        {"4US", "BMW USA"},
                        {"4UZ", "Frt-Thomas Bus"},
                        {"4V1", "Volvo"},
                        {"4V2", "Volvo"},
                        {"4V3", "Volvo"},
                        {"4V4", "Volvo"},
                        {"4V5", "Volvo"},
                        {"4V6", "Volvo"},
                        {"4VL", "Volvo"},
                        {"4VM", "Volvo"},
                        {"4VZ", "Volvo"},
                        {"538", "Zero Motorcycles (USA)"},
                        {"5F", "Honda USA-Alabama"},
                        {"5L", "Lincoln"},
                        {"5N1", "Nissan USA"},
                        {"5NP", "Hyundai USA"},
                        {"5T", "Toyota USA - trucks"},
                        {"5YJ", "Tesla Motors"},
                        {"6AB", "MAN Australia"},
                        {"6F4", "Nissan Motor Company Australia"},
                        {"6F5", "Kenworth Australia"},
                        {"6FP", "Ford Motor Company Australia"},
                        {"6G1", "General Motors-Holden (post Nov 2002)"},
                        {"6G2", "Pontiac Australia (GTO & G8)"},
                        {"6H8", "General Motors-Holden (pre Nov 2002)"},
                        {"6MM", "Mitsubishi Motors Australia"},
                        {"6T1", "Toyota Motor Corporation Australia"},
                        {"6U9", "Privately Imported car in Australia"},
                        {"8AD", "Peugeot Argentina"},
                        {"8AF", "Ford Motor Company Argentina"},
                        {"8AG", "Chevrolet Argentina"},
                        {"8AJ", "Toyota Argentina"},
                        {"8AK", "Suzuki Argentina"},
                        {"8AP", "Fiat Argentina"},
                        {"8AW", "Volkswagen Argentina"},
                        {"8A1", "Renault Argentina"},
                        {"8GD", "Peugeot Chile"},
                        {"8GG", "Chevrolet Chile"},
                        {"935", "Citroën Brazil"},
                        {"936", "Peugeot Brazil"},
                        {"93H", "Honda Brazil"},
                        {"93R", "Toyota Brazil"},
                        {"93U", "Audi Brazil"},
                        {"93V", "Audi Brazil"},
                        {"93X", "Mitsubishi Motors Brazil"},
                        {"93Y", "Renault Brazil"},
                        {"94D", "Nissan Brazil"},
                        {"9BD", "Fiat Brazil"},
                        {"9BF", "Ford Motor Company Brazil"},
                        {"9BG", "Chevrolet Brazil"},
                        {"9BM", "Mercedes-Benz Brazil"},
                        {"9BR", "Toyota Brazil"},
                        {"9BS", "Scania Brazil"},
                        {"9BW", "Volkswagen Brazil"},
                        {"9FB", "Renault Colombia"}
                    };

                    }
                }
                return _wmi;
            }
        }

        public static bool IsValid(string vin) {

            var value = 0;

            if (vin?.Length != ValidVinLength) {
                return false;
            }

            var checkCharacter = vin[CheckDigitIndex];
            if (!ValidCheckCharacters.ContainsKey(checkCharacter)) {
                return false;
            }

            for (var i = 0; i < ValidVinLength; i++) {
                if (!CharacterTransliteration.ContainsKey(vin[i])) {
                    return false;
                }
                value += (CharacterWeights[i] * (CharacterTransliteration[vin[i]]));
            }

            return (value % 11) == ValidCheckCharacters[checkCharacter];
        }

        public static string GetWorldManufacturer(string vinOrWmi) {
            if (string.IsNullOrEmpty(vinOrWmi))
                return string.Empty;

            if (vinOrWmi.Length < 2)
                return string.Empty;

            if (vinOrWmi.Length > 2 && WorldManufacturerIdentifiers.ContainsKey(vinOrWmi.Substring(0, 3))) {
                return WorldManufacturerIdentifiers[vinOrWmi.Substring(0, 3)];
            }

            return WorldManufacturerIdentifiers.ContainsKey(vinOrWmi.Substring(0, 2)) ? WorldManufacturerIdentifiers[vinOrWmi.Substring(0, 2)] : string.Empty;
        }

        public static int GetModelYear(char yearCharacter, int startYear = 0) {
            if (startYear == 0) {
                startYear = DefaultYear;
            }

            if (Years.ContainsKey(yearCharacter)) {
                var year = startYear + Years[yearCharacter];
                if (year > NextYear) {
                    year -= 30;
                }
                return year;
            }

            return 0;

        }

        public static int GetModelYear(string vin, int startYear = 0) {

            if (string.IsNullOrEmpty(vin))
                return 0;

            if (vin.Length < 10) {
                return 0;
            }

            var yearCharacter = vin[9];

            return GetModelYear(yearCharacter, startYear);
        }

    }
}
