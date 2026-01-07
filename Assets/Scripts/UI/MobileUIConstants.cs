using UnityEngine;

namespace Shakki.UI
{
    /// <summary>
    /// Constants for mobile-friendly UI design.
    /// Based on Apple Human Interface Guidelines and Material Design specs.
    /// </summary>
    public static class MobileUIConstants
    {
        // Minimum touch target size (44pt iOS / 48dp Android)
        public const float MinTouchTargetSize = 88f; // Doubled for high DPI

        // Recommended button sizes
        public const float PrimaryButtonWidth = 300f;
        public const float PrimaryButtonHeight = 88f;
        public const float SecondaryButtonWidth = 200f;
        public const float SecondaryButtonHeight = 72f;
        public const float IconButtonSize = 88f;

        // Text sizes (for 1080p reference resolution)
        public const int TitleFontSize = 72;
        public const int HeadingFontSize = 48;
        public const int SubheadingFontSize = 36;
        public const int BodyFontSize = 28;
        public const int CaptionFontSize = 24;

        // Spacing
        public const float LargeSpacing = 40f;
        public const float MediumSpacing = 24f;
        public const float SmallSpacing = 12f;

        // Canvas scaler reference resolution (portrait mobile)
        public static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        // Safe padding from edges
        public const float EdgePadding = 32f;

        /// <summary>
        /// Configures a CanvasScaler for mobile-first responsive design.
        /// </summary>
        public static void ConfigureCanvasScaler(UnityEngine.UI.CanvasScaler scaler)
        {
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balance between width and height
        }
    }
}
