using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CRMS_Peguit.Models
{
    internal class AzureShades
    {
        public static readonly Color PressedAzure = Color.FromArgb(12, 77, 130);  // 10% black - Deep Harbor
        public static readonly Color ActiveBlue = Color.FromArgb(10, 69, 116);  // 20% black - Night Water
        public static readonly Color OverlayBlue = Color.FromArgb(9, 60, 102);   // 30% black - Abyss
        public static readonly Color DeepInterface = Color.FromArgb(8, 52, 87);    // 40% black - Wet Slate
        public static readonly Color ContrastAzure = Color.FromArgb(6, 43, 72);    // 50% black - Storm Sea
        public static readonly Color HighEmphasisBlue = Color.FromArgb(5, 34, 58);    // 60% black - Deep Basalt
        public static readonly Color MaxContrastBlue = Color.FromArgb(4, 26, 44);    // 70% black - Charred Wood
        public static readonly Color NearBlackAzure = Color.FromArgb(3, 17, 29);    // 80% black - Coal
        public static readonly Color TrueBlackAltAzure = Color.FromArgb(1, 9, 14);     // 90% black - Jet Black
        public static readonly Color TrueBlack = Color.FromArgb(0, 0, 0);      // 100% black - Obsidian
    }
}