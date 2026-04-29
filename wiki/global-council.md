# Global Council (Мировой Совет)

Глобальная организация где все альянсы (и опционально одиночные города) могут голосовать за решения касающиеся всего сервера.

## Участники

- Все альянсы автоматически являются членами
- Одиночные города (без альянса) — опционально, решается конфигом `COUNCIL_SOLO_CITIES_CAN_VOTE`
- Минимальный размер для участия — конфиг `COUNCIL_MIN_CITIES_TO_PARTICIPATE`

## Вес голоса

Конфиг `COUNCIL_VOTE_WEIGHT_MODE`:
- `EQUAL` — у каждого члена 1 голос
- `BY_CITIES` — вес = количество городов в альянсе
- `BY_PLOTS` — вес = суммарное количество плотов

## Типы решений (CouncilMotionType)

| Тип | Эффект | Требует голосов |
|-----|--------|-----------------|
| `SANCTIONS` | Запрет торговли с целевым альянсом/городом | Простое большинство |
| `PARIAH` | Объявить альянс вне закона — все могут атаковать без обычных требований к конфликту | Супербольшинство (2/3) |
| `CEASEFIRE` | Принудительный мир на X дней — никто не может объявлять войны | Супербольшинство (2/3) |
| `COALITION` | Все члены совета объявляют войну одному альянсу | Супербольшинство (2/3) |
| `AMNESTY` | Снять статус PARIAH с кого-то | Простое большинство |
| `LAW` | Изменить сервер-параметр (штраф за агрессию и т.п.) | Супербольшинство (2/3) |

### Возможные типы LAW

| Закон | Эффект |
|-------|--------|
| `TAX_ON_WAR` | Победитель войны платит налог совету (% от захваченного) |
| `CITY_FOUND_RADIUS` | Запрет основывать города в радиусе N от чужих |
| `MIN_CITY_SIZE_FOR_ALLIANCE` | Минимум городов для создания альянса |
| `WAR_COOLDOWN` | Минимальный cooldown между войнами |
| `SMALL_CITY_PROTECTION` | Запрет атаковать города с населением < N |
| `QUORUM_CHANGE` | Изменить порог кворума совета |
| `VOTE_DURATION_CHANGE` | Изменить длительность голосования |
| `VOTE_WEIGHT_CHANGE` | Изменить режим весов голосов |
| `PLOT_PRICE_MIN` | Минимальная цена продажи плота |
| `DECLARE_WAR_COST` | Объявление войны стоит X ресурсов (антиспам) |
| `COUNCIL_TAX` | Все альянсы платят % в казну совета каждый игровой месяц |
| `ALLIANCE_DEBT_LIMIT` | Альянс не может объявлять войну если в долгах |
| `MAX_PLOTS_PER_CITY` | Лимит плотов на один город |
| `CLAIM_EXPANSION_RATE` | Альянс может клеймить не больше N плотов в день |
| `PEACE_MIN_DURATION` | После войны мир минимум X дней |
| `ENCLAVE_BAN` | Нельзя клеймить плоты полностью окружённые чужими |
| `ANNEXATION_PLOT_LIMIT` | За одну войну можно забрать не больше N плотов |
| `ALLY_LIMIT` | Максимум N официальных союзников у альянса |
| `ALLY_WAR_DRAG` | Союзники автоматически втягиваются в войну (или запрет этого) |
| `ALLIANCE_REFORM_COOLDOWN` | Распущенный альянс нельзя пересоздать X дней |
| `NEUTRAL_CITY_IMMUNITY` | Города без альянса нельзя атаковать |

## Кворум

- `COUNCIL_QUORUM_PERCENT` — минимальный % членов проголосовавших для легитимности решения (например 50%)
- Если кворум не набран к дедлайну — решение не принято

## Процесс

1. Любой член предлагает решение (`/council propose <type> <target> <reason>`)
2. Открывается период голосования — `COUNCIL_VOTE_DURATION_DAYS` дней
3. Члены голосуют `/council vote <motionId> yes/no`
4. По истечении срока — автоматический подсчёт
5. Если кворум и большинство достигнуты — эффект применяется в коде автоматически

## Вето

- Целевая сторона (против кого голосуют) не может голосовать по данному решению
- Опционально: `COUNCIL_ALLOW_VETO` — один из крупнейших альянсов может заблокировать (1 раз в N дней)

## Архитектура кода

```
src/part/structure/council/
    GlobalCouncil.cs        — синглтон, хранит список активных решений
    CouncilMotion.cs        — тип, инициатор, цель, голоса, дедлайн, статус
    CouncilMotionType.cs    — enum типов
    CouncilMotionStatus.cs  — ACTIVE, PASSED, FAILED, VETOED
    CouncilEffectHandler.cs — применяет эффекты прошедших решений
    CouncilVoteWeight.cs    — enum режима веса голоса
```

```csharp
public class CouncilMotion {
    public string Guid { get; set; }
    public CouncilMotionType Type { get; set; }
    public string ProposerGuid { get; set; }      // guid альянса/города
    public string TargetGuid { get; set; }         // против кого
    public Dictionary<string, int> Votes { get; set; } // memberGuid → вес голоса (положительный = за, отрицательный = против)
    public DateTime Deadline { get; set; }
    public CouncilMotionStatus Status { get; set; }
    public object MotionData { get; set; }         // доп. параметры (длительность перемирия и т.п.)
}
```

`CouncilEffectHandler` применяет эффект при `Status = PASSED`:
- `SANCTIONS` → добавляет запись в `GlobalCouncil.ActiveSanctions`
- `PARIAH` → устанавливает флаг на альянсе/городе, снимает требования к конфликту
- `CEASEFIRE` → устанавливает `GlobalCouncil.CeasefireUntil`, блокирует объявление войн
- `COALITION` → автоматически создаёт конфликты между членами и целью

## Конфиг

```csharp
public bool COUNCIL_ENABLED = false;
public bool COUNCIL_SOLO_CITIES_CAN_VOTE = false;
public int COUNCIL_MIN_CITIES_TO_PARTICIPATE = 1;
public CouncilVoteWeightMode COUNCIL_VOTE_WEIGHT_MODE = CouncilVoteWeightMode.EQUAL;
public int COUNCIL_VOTE_DURATION_DAYS = 3;
public int COUNCIL_QUORUM_PERCENT = 50;
public bool COUNCIL_ALLOW_VETO = false;
public int COUNCIL_VETO_COOLDOWN_DAYS = 7;
```

## Интеграция

- Объявление войны → проверить `GlobalCouncil.CeasefireUntil` и `ActiveSanctions`
- Создание конфликта → проверить статус `PARIAH` у цели
- `COALITION` → `CouncilEffectHandler` создаёт ConflictLetters от всех членов к цели
