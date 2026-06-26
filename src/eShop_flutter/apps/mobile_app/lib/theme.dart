import 'package:flutter/material.dart';

class AppTheme {
  static const Color background = Color(0xFFF8FAFC); // Slate 50
  static const Color cardBg = Color(0xFFFFFFFF); // White
  static const Color textPrimary = Color(0xFF0F172A); // Slate 900
  static const Color textSecondary = Color(0xFF475569); // Slate 600
  static const Color textMuted = Color(0xFF94A3B8); // Slate 400
  static const Color border = Color(0xFFE2E8F0); // Slate 200
  
  static const Color primary = Color(0xFF0891B2); // Cyan 600
  static const Color primaryLight = Color(0xFFECFEFF); // Cyan 50
  
  static const Color success = Color(0xFF16A34A); // Green 600
  static const Color error = Color(0xFFDC2626); // Red 600
  
  static const TextStyle heading1 = TextStyle(
    color: textPrimary,
    fontSize: 28,
    fontWeight: FontWeight.bold,
    letterSpacing: -0.5,
  );
  
  static const TextStyle heading2 = TextStyle(
    color: textPrimary,
    fontSize: 20,
    fontWeight: FontWeight.bold,
    letterSpacing: -0.2,
  );

  static const TextStyle body = TextStyle(
    color: textPrimary,
    fontSize: 15,
  );

  static const TextStyle bodySecondary = TextStyle(
    color: textSecondary,
    fontSize: 13,
  );

  static ThemeData get themeData {
    return ThemeData(
      scaffoldBackgroundColor: background,
      primaryColor: primary,
      colorScheme: const ColorScheme.light(
        primary: primary,
        secondary: primary,
        surface: cardBg,
        error: error,
      ),
    );
  }
}
