using System.Collections.Generic;
using UnityEngine;
using System;

public class TurnOrderController : MonoBehaviour
{
    [SerializeField] private SpawnPositions spawns;
    private readonly Queue<CharacterScript> _queue = new();
    private readonly List<CharacterScript> _forecast = new();

    private CharacterScript _current;

    public IReadOnlyList<CharacterScript> Forecast => _forecast;
    public CharacterScript Current => _current;

    public event Action ForecastChanged;

    public void Initialize()
    {
        _queue.Clear();
        _forecast.Clear();
        _current = null;

        var all = new List<CharacterScript>();
        all.AddRange(spawns.Players);
        all.AddRange(spawns.Enemies);

        all.Sort((a, b) => b.GetAGI().CompareTo(a.GetAGI()));
        foreach (var u in all) if (u) _queue.Enqueue(u);
        RebuildForecast();
    }

    public CharacterScript PopNext()
    {
        if (_queue.Count == 0) return null;
        _current = _queue.Dequeue();
        RebuildForecast();
        return _current;
    }

    public void Requeue(CharacterScript unit)
    {
        if (!unit) return;
        _queue.Enqueue(unit);
        if (_current == unit) _current = null;
        RebuildForecast();
    }

    private void RebuildForecast()
    {
        _forecast.Clear();
        if (_current != null) _forecast.Add(_current);
        _forecast.AddRange(_queue);
        ForecastChanged?.Invoke();
    }
}
