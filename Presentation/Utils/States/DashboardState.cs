using OptimusPrime.Core.Utils;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Presentation.Services;
using OptimusPrime.Presentation.Utils.Storage;

namespace OptimusPrime.Presentation.Utils.States;

public enum CardDisplayed { Login, Register, Refresh, Instruction, Audit, Email, ForgotPassword, ResetPassword, AppVersion }
public enum StatoRichiesta { Successo, Fallimento, Elaborazione }

public class DashboardState (
    ILogger<DashboardState> logger,
    JwtTimerState jwtTimerState,
    AuthService userService,
    ProfileService profileService,
    SessionStorage sessionStorage ) {

    private readonly ILogger<DashboardState> _logger = logger;
    private readonly JwtTimerState _jwtTimerState = jwtTimerState;
    private readonly AuthService _userService = userService;
    private readonly ProfileService _profileService = profileService;
    private readonly SessionStorage _sessionStorage = sessionStorage;

    public event Action? OnChange;
    public CardDisplayed CardState { get; set; } = CardDisplayed.Login;
    public StatoRichiesta StatoRichiesta { get; set; } = StatoRichiesta.Elaborazione;
    public bool IsApiMode { get; set; } = true;
    public bool? IsAdmin { get; set; } = null;
    public bool IsDrawerOpen { get; set; } = false;

    // ==========================
    // Dashboard states
    // ==========================

    public void SetCardState(CardDisplayed state) {
        if(CardState == state) return;

        CardState = state;
        OnChange?.Invoke();
    }

    public void SetRichiestaState(StatoRichiesta state) {
        if(StatoRichiesta == state) return;

        StatoRichiesta = state;
        OnChange?.Invoke();
    }

    public void SetApiMode(bool isApi) {
        if(IsApiMode == isApi)
            return;

        IsApiMode = isApi;
        OnChange?.Invoke();
    }

    public void ToggleDrawer() {
        IsDrawerOpen = !IsDrawerOpen;
        OnChange?.Invoke();
    }

    // ==========================
    // User states
    // ==========================

    // controlla se l'utente è admin e aggiorna lo stato di conseguenza
    public async Task CheckRole() {
        try {
            JwtTokenModel? jwt = await _sessionStorage.GetToken();
            if(jwt == null || string.IsNullOrEmpty(jwt.AccessToken) || string.IsNullOrEmpty(jwt.RefreshToken)) {
                _logger.LogWarning("Token JWT non trovato o incompleto. Impossibile verificare il ruolo dell'utente.");
                IsAdmin = null;
                return;
            }

            Result<UserModel> result = await _profileService.Me();

            if(result == null || !result.IsSuccess || result.Value == null) {
                _logger.LogError("Impossibile ottenere i dati dell'utente: {Message}", result?.Message);
                await Logout();
                return;
            } 

            IsAdmin = result.Value.Role == Privileges.Admin;

        } catch(Exception ex) {
            _logger.LogError($"Errore durante il controllo del ruolo: {ex.Message}");
            IsAdmin = null;

        } finally {
            OnChange?.Invoke();
        }
    }

    // effettua il logout
    public bool IsLogoutInProgress = false;
    public async Task Logout() {
        if(IsLogoutInProgress)
            return;

        if(!IsAdmin.HasValue)
            return;

        IsLogoutInProgress = true;

        bool jwtResult = await _sessionStorage.ClearTokens();

        if(jwtResult) {
            IsAdmin = null; 
            _jwtTimerState.SetTimer(0, 0);

            if(CardState == CardDisplayed.Audit) {
                SetCardState(CardDisplayed.Login);
            }

            _logger.LogInformation("Logout effettuato con successo");

        } else {
            _logger.LogError("Impossibile cancellare i token");
        }

        IsLogoutInProgress = false;
        OnChange?.Invoke();
    }
}
