using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Presentation.Utils.States;

public class HealthState {
    public List<HealthModel> Servizi { get; private set; } = [];

    public event Action? OnChange;

    public void UpdateHealthState(List<HealthModel> nuoviServizi) {
        Servizi = nuoviServizi;
        OnChange?.Invoke(); 
    }
}