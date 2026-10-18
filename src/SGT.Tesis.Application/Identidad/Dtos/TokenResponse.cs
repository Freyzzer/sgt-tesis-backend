namespace SGT.Tesis.Application.Identidad.Dtos;

public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiraEn,
    string RefreshToken,
    DateTime RefreshTokenExpiraEn,
    string TipoToken = "Bearer");
