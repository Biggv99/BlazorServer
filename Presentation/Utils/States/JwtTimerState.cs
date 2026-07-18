namespace OptimusPrime.Presentation.Utils.States;

public class JwtTimerState() {

    public TimeSpan TimeRemainingAccess { get; private set; } = TimeSpan.Zero;
    public TimeSpan TimeRemainingRefresh { get; private set; } = TimeSpan.Zero;

    public event Action? OnChange;

    // imposta un valore di secondi al timer
    public void SetTimer(int access, int refresh) {
        TimeRemainingAccess = TimeSpan.FromSeconds(access);
        TimeRemainingRefresh = TimeSpan.FromSeconds(refresh);
        OnChange?.Invoke();
    }

    // decrementa ogni secondo
    public void DecrementaSecondi() {
        if(TimeRemainingAccess.TotalSeconds > 0)
            TimeRemainingAccess = TimeRemainingAccess.Subtract(TimeSpan.FromSeconds(1));

        if(TimeRemainingRefresh.TotalSeconds > 0)
            TimeRemainingRefresh = TimeRemainingRefresh.Subtract(TimeSpan.FromSeconds(1));
    }
}