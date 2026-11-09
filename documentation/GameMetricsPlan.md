# Pixel Rebellion: Shadow Protocol — Gameplay Metrics Plan

This document outlines the player interaction data that will be collected during gameplay and usability testing. The collected data will be used to evaluate player performance, compare Easy and Hard difficulty modes, identify areas where players may struggle, and support improvements to the overall gameplay experience.

The results will help determine whether the game is appropriately balanced and whether players understand the stealth mechanics, objectives, and controls.

---

## Goals
The gameplay metrics will be used to:

- Measure player performance.
- Compare Easy and Hard difficulty modes.
- Identify mechanics that may be too easy or too difficult.
- Identify areas where players frequently get detected or lose lives.
- Determine whether players understand the stealth and hiding mechanics.
- Determine whether players can locate and collect required evidence.
- Measure how long players take to complete the level.
- Support recommendations for Version 2 improvements.

---

## Game Metrics

| Metric                | Description                                                           | Reason for Tracking                                                   |
| --------------------- | --------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Difficulty Selected   | Records whether the player chooses Easy or Hard mode.                 | Allows comparison between difficulty levels.                          |
| Level Completed       | Records whether the player successfully reaches the extraction point. | Measures overall success rate.                                        |
| Completion Time       | Records the total time taken to finish the level.                     | Helps determine whether the level takes too long or is too difficult. |
| Detection Count       | Records each time the player is detected by a guard or camera.        | Measures how difficult stealth mechanics are.                         |
| Detection Source      | Records whether detection came from a guard or security camera.       | Helps determine which security mechanic causes more difficulty.       |
| Lives Lost            | Records the number of lives lost during gameplay.                     | Measures player struggle and difficulty.                              |
| Evidence Collected    | Records the number of evidence items collected.                       | Determines whether players understand and complete objectives.        |
| Evidence Missed       | Records evidence not collected before the player fails or exits.      | Helps identify evidence that may be difficult to locate.              |
| Hiding Count          | Records how many times the player uses a hiding location.             | Measures whether players understand and use the hiding mechanic.      |
| Terminal Interactions | Records how many times players interact with security terminals.      | Measures use and understanding of the terminal mechanic.              |
| Alert Level           | Records the player's alert/detection level during gameplay.           | Measures overall stealth performance.                                 |
| Final Score           | Records the player's final score.                                     | Provides an overall performance measurement.                          |
| Restart Count         | Records how many times the player restarts the level.                 | Helps identify frustration or difficulty.                             |

---

## Gameplay Events

| Gameplay Event                      | Data Recorded                                   |
| ----------------------------------- | ----------------------------------------------- |
| Player starts game                  | Difficulty selected                             |
| Player enters guard vision          | Detection count, detection source               |
| Player enters camera detection zone | Detection count, detection source               |
| Player uses hiding location         | Hiding count                                    |
| Player collects evidence            | Evidence collected                              |
| Player interacts with terminal      | Terminal interaction count                      |
| Player loses a life                 | Lives lost                                      |
| Player restarts                     | Restart count                                   |
| Player reaches extraction           | Completion status, completion time, final score |
| Player fails level                  | Completion status, completion time, lives lost  |

---

## Data Collection Format

> Gameplay data will be stored in CSV format so that it can be easily reviewed and analyzed using Microsoft Excel or Google Sheets. Each gameplay session will represent one row of data.
>
> Example:

| Player ID | Difficulty | Time(s) | Detections | Lives Lost | Evidence | Hides | Score | Completed |
| --------- | ---------- | ---: | ---------: | ---------: | -------: | ----: | ----: | --------- |
| P001      | Easy       |  185 |          2 |          0 |        4 |     3 |  1250 | Yes       |
| P002      | Hard       |  310 |          7 |          2 |        3 |     8 |   720 | No        |

---

## Easy vs. Hard Comparison
> Gameplay metrics will be compared between Easy and Hard mode to determine whether Hard mode noticeably increases difficulty without causing excessive frustration or usability errors.

| Metric          | Expected Result                                  |
| --------------- | ------------------------------------------------ |
| Completion Time | Hard should take longer than Easy                |
| Detection Count | Hard should have more detections                 |
| Lives Lost      | Hard should have more lives lost                 |
| Completion Rate | Easy should have a higher completion rate        |
| Final Score     | Easy may result in higher average scores         |
| Hiding Count    | Hard may require players to hide more frequently |

---

## Difficulty Model
> Score Formula = (Detection Count × 2) + (Lives Lost × 3) + (Restart Count × 2)

| Score | Player Experience |
| ----: | ----------------- |
|   0–5 | Low struggle      |
|  6–12 | Moderate struggle |
|   13+ | High struggle     |

---

## Analysis Plan

> After usability testing, the collected gameplay data will be analyzed using descriptive statistics. Easy and Hard mode results will be compared to identify differences in player performance.

Future Calculations:

- Average completion time
- Average detection count
- Average lives lost
- Average evidence collected
- Average score
- Completion rate
- Average struggle score

---

## Planned Visualizations

- Bar chart comparing Easy vs. Hard completion times.
- Bar chart comparing Easy vs. Hard detection counts.
- Bar chart comparing completion rates.
- Chart comparing average struggle scores.
- Table summarizing all usability-test results.

## How Results Will Improve the Game
> The gameplay metrics will be used alongside usability-testing observations to identify gameplay problems. If the data shows that Hard mode is excessively difficult, the team may adjust guard speed, detection sensitivity, safe-zone size, or the number of player lives. If players repeatedly miss evidence or avoid certain mechanics, the team may improve visual indicators, instructions, or feedback.
