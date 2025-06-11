using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TournamentSystem.Data;
using TournamentSystem.Models;
using System.Security.Claims;
using TournamentSystem.ViewModels;
using Microsoft.AspNetCore.SignalR;
using TournamentSystem.Hubs;


namespace TournamentSystem.Controllers
{
    public class TournamentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<MatchHub> _hubContext;

        public TournamentController(ApplicationDbContext context, IHubContext<MatchHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index(string? search, string? sort, string? direction, string? statusFilter, int page = 1)
        {
            const int PageSize = 10;

            // Automatyczna aktualizacja statusów turniejów
            var upcomingTournamentsToUpdate = await _context.Tournaments
                .Where(t => t.Status == TournamentStatus.Upcoming &&
                            t.ApplicationDeadline < DateTime.Now &&
                            !_context.Matches.Any(m => m.TournamentId == t.Id))
                .ToListAsync();

            foreach (var t in upcomingTournamentsToUpdate)
            {
                await TryGenerateLadder(t.Id);
            }

            var query = _context.Tournaments
                .Include(t => t.Participants)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.Name.ToLower().Contains(search.ToLower()) ||
                    t.Discipline.ToLower().Contains(search.ToLower()));
            }
            
            // Filtrowanie po statusie
            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = statusFilter.ToLower() switch
                {
                    "upcoming" => query.Where(t => t.Status == TournamentStatus.Upcoming),
                    "active" => query.Where(t => t.Status == TournamentStatus.Active),
                    "finished" => query.Where(t => t.Status == TournamentStatus.Finished),
                    _ => query
                };
            }

            // Sorting logic
            bool desc = direction == "desc";
            query = sort switch
            {
                "name" => desc ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "discipline" => desc ? query.OrderByDescending(t => t.Discipline) : query.OrderBy(t => t.Discipline),
                "date" => desc ? query.OrderByDescending(t => t.Date) : query.OrderBy(t => t.Date),
                "applicationDeadline" => desc ? query.OrderByDescending(t => t.ApplicationDeadline) : query.OrderBy(t => t.ApplicationDeadline),
                _ => query.OrderBy(t => t.Date)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            var tournaments = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var viewModel = new PaginatedTournamentViewModel
            {
                Tournaments = tournaments,
                CurrentFilter = search,
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentSort = sort
            };

            return View(viewModel);
        }




        // GET: /Tournament/Details/5
        public async Task<IActionResult> Details(int id, string? search, string? sort, string? direction,int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return NotFound();

            // Automatyczne rozpoczęcie, jeśli upłynął deadline i brak drabinki
            if (tournament.Status == TournamentStatus.Upcoming &&
                tournament.ApplicationDeadline < DateTime.Now &&
                !_context.Matches.Any(m => m.TournamentId == tournament.Id))
            {
                await GenerateLadder(tournament.Id);

                tournament = await _context.Tournaments
                    .Include(t => t.Participants).ThenInclude(p => p.User)
                    .Include(t => t.Matches)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }

            var participants = tournament.Participants.ToList();
            var isParticipant = participants.Any(p => p.UserId == userId);
            var isOrganizer = tournament.OrganizerId == userId;

            var matches = await _context.Matches
                .Where(m => m.TournamentId == tournament.Id)
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .OrderBy(m => m.Round)
                .ToListAsync();

            var viewModel = new TournamentDetailsViewModel
            {
                Tournament = tournament,
                Participants = participants,
                Matches = matches,
                IsUserParticipant = isParticipant,
                IsOrganizer = isOrganizer,
            };

            // Zapamiętaj kontekst, aby umożliwić powrót do właściwego miejsca
            ViewData["CurrentFilter"] = search;
            ViewData["CurrentSort"] = sort;
            ViewData["CurrentDirection"] = direction;
            ViewData["CurrentPage"] = page;

            return View(viewModel);
        }


        // GET: /Tournament/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Tournament/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(Tournament tournament)
        {
            tournament.OrganizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                if (tournament.Date < DateTime.Now)
                {
                    ModelState.AddModelError("Date", "Tournament date cannot be in the past.");
                    return View(tournament);
                }

                if (tournament.ApplicationDeadline < DateTime.Now)
                {
                    ModelState.AddModelError("ApplicationDeadline", "Deadline zgłoszeń nie może być w przeszłości.");
                    return View(tournament);
                }

                if (tournament.ApplicationDeadline > tournament.Date)
                {
                    ModelState.AddModelError("ApplicationDeadline", "Deadline zgłoszeń nie może być po dacie turnieju.");
                    return View(tournament);
                }

                _context.Tournaments.Add(tournament);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tournament);
        }

        // GET: Tournament/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (tournament.OrganizerId != userId)
            {
                TempData["ErrorMessage"] = "Nie masz uprawnień do edycji tego turnieju.";
                return RedirectToAction("Details", new { id = tournament.Id });
            }

            return View(tournament);
        }

        // POST: Tournament/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tournament tournament)
        {
            if (id != tournament.Id) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var originalTournament = await _context.Tournaments.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (originalTournament == null) return NotFound();

            if (originalTournament.OrganizerId != userId)
            {
                TempData["ErrorMessage"] = "Nie masz uprawnień do edycji tego turnieju.";
                return RedirectToAction("Details", new { id });
            }
            if (originalTournament.Status != TournamentStatus.Upcoming)
            {
                TempData["ErrorMessage"] = "Zmieny nie zostały wprowadzone - można edytować tylko nadchodzące turnieje.";
                return RedirectToAction("Details", new { id });
            }
            if (tournament.Date < DateTime.Now)
            {
                ModelState.AddModelError("Date", "Tournament date cannot be in the past.");
                return View(tournament);
            }

            if (tournament.ApplicationDeadline < DateTime.Now)
            {
                ModelState.AddModelError("ApplicationDeadline", "Deadline zgłoszeń nie może być w przeszłości.");
                return View(tournament);
            }

            if (tournament.ApplicationDeadline > tournament.Date)
            {
                ModelState.AddModelError("ApplicationDeadline", "Deadline zgłoszeń nie może być po dacie turnieju.");
                return View(tournament);
            }
            if (!ModelState.IsValid)
                return View(tournament);

            try
            {
                
                tournament.OrganizerId = originalTournament.OrganizerId;

                _context.Update(tournament);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tournaments.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Details), new { id = tournament.Id });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Apply(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tournament == null) return NotFound();

            if (tournament.Participants.Count >= tournament.MaxParticipants)
                return BadRequest("Limit uczestników został osiągnięty.");
            if (DateTime.Now > tournament.ApplicationDeadline)
            {
                TempData["ErrorMessage"] = "Zapisy do turnieju zostały zakończone.";
                return RedirectToAction("Details", new { id });
            }


            var model = new ApplyViewModel
            {
                TournamentId = id,
                TournamentName = tournament.Name
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyViewModel model)
        {
            Console.WriteLine($"Model: License={model.LicenseNumber}, Ranking={model.Ranking}");

            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == model.TournamentId);

            if (tournament == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (tournament.Participants.Any(p => p.UserId == userId))
            {
                ModelState.AddModelError("", "Jesteś już zapisany na ten turniej.");
            }

            if (tournament.Participants.Count >= tournament.MaxParticipants)
            {
                ModelState.AddModelError("", "Limit uczestników został osiągnięty.");
            }
            if (DateTime.Now > tournament.ApplicationDeadline)
            {
                return RedirectToAction("Details", new { id = model.TournamentId });
            }


            var licenseExists = await _context.Participants.AnyAsync(p => p.LicenseNumber == model.LicenseNumber);
            var rankingExists = await _context.Participants.AnyAsync(p => p.Ranking == model.Ranking);

            if (licenseExists)
            {
                ModelState.AddModelError("LicenseNumber", "Ten numer licencji jest już używany.");
            }

            if (rankingExists)
            {
                ModelState.AddModelError("Ranking", "Ten ranking jest już przypisany innemu uczestnikowi.");
            }

            if (!ModelState.IsValid)
            {
                model.TournamentName = tournament.Name;
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"Field: {key} => {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            var participant = new Participant
            {
                TournamentId = tournament.Id,
                UserId = userId,
                LicenseNumber = model.LicenseNumber,
                Ranking = model.Ranking
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = tournament.Id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int tournamentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tournament = await _context.Tournaments.FindAsync(tournamentId);

            if (tournament == null)
                return NotFound();

            if (tournament.Status != TournamentStatus.Upcoming)
            {
                TempData["ErrorMessage"] = "Nie można wycofać się z turnieju, który już się rozpoczął.";
                return RedirectToAction("Details", new { id = tournamentId });
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.TournamentId == tournamentId && p.UserId == userId);

            if (participant == null)
                return NotFound();

            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Pomyślnie wycofano się z turnieju.";
            return RedirectToAction("Details", new { id = tournamentId });
        }

        [Authorize]
        public async Task<IActionResult> MyTournaments()
        {
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Forbid();
            }
            var now = DateTime.Now;

            var viewModel = new MyTournamentsViewModel();

            // 1. Get Organized Tournaments
            viewModel.Organized = await _context.Tournaments
                .Where(t => t.OrganizerId == userId)
                .OrderBy(t => t.Date)
                .ToListAsync();

            // 2. Get Participated Tournaments and Upcoming Matches
            var participatedTournaments = await _context.Participants
                .Where(p => p.UserId == userId)
                .Include(p => p.Tournament) 
                    .ThenInclude(t => t.Participants) 
                        .ThenInclude(participant => participant.User) 
                .Include(p => p.Tournament) 
                    .ThenInclude(t => t.Matches) 
                        .ThenInclude(match => match.Player1) 
                .Include(p => p.Tournament) 
                    .ThenInclude(t => t.Matches) 
                        .ThenInclude(match => match.Player2) 
                .Select(p => p.Tournament)
                .OrderBy(t => t.Date)
                .ToListAsync();

            foreach (var tournament in participatedTournaments)
            {
                if (tournament.Status == TournamentStatus.Upcoming)
                {
                    viewModel.Upcoming.Add(tournament);
                }
                else if (tournament.Status == TournamentStatus.Active)
                {
                    viewModel.Active.Add(tournament);

                    // Add upcoming matches for the current user within this active tournament
                    foreach (var match in tournament.Matches)
                    {
                        if ((match.Player1Id == userId || match.Player2Id == userId) && match.WinnerId == null)
                        {
                            viewModel.UpcomingMatches.Add(match);
                        }
                    }
                }
                else if (tournament.Status == TournamentStatus.Finished)
                {
                    viewModel.Past.Add(tournament);
                }
            }
            
            viewModel.UpcomingMatches = viewModel.UpcomingMatches
                                        .OrderBy(m => m.Tournament.Date)
                                        .ThenBy(m => m.Round)
                                        .ToList();

            return View(viewModel);
        }

        private async Task<bool> TryGenerateLadder(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            var players = tournament.Participants.OrderBy(p => p.Ranking).ToList();
            if (players.Count < 2)
            {
                tournament.Status = TournamentStatus.Finished;
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group(tournament.Id.ToString()).SendAsync("tournamentStatusUpdated", tournament.Id);
                return false;
            }

            if (tournament.Matches.Any())
                _context.Matches.RemoveRange(tournament.Matches);

            tournament.Status = TournamentStatus.Active;

            int totalPlayers = players.Count;
            int totalSlots = (int)Math.Pow(2, Math.Ceiling(Math.Log2(totalPlayers)));
            int totalRounds = (int)Math.Log2(totalSlots);

            var matches = new Dictionary<(int round, int index), Match>();

            // Inicjalizacja meczów
            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound = (int)Math.Pow(2, totalRounds - round);
                for (int i = 0; i < matchesInRound; i++)
                {
                    var match = new Match
                    {
                        TournamentId = tournament.Id,
                        Round = round
                    };
                    matches[(round, i)] = match;
                    _context.Matches.Add(match);
                }
            }

            int freeSlots = totalSlots - totalPlayers;
            int firstMatchRound = 1 + (int)Math.Log2(freeSlots + 1);

            // Przypisz graczy do pierwszej rundy
            for (int i = 0; i < totalSlots; i += 2)
            {
                string? p1 = i < totalPlayers ? players[i].UserId : null;
                string? p2 = (i + 1) < totalPlayers ? players[i + 1].UserId : null;

                int matchIndex = i / 2;
                var match = matches[(1, matchIndex)];

                match.Player1Id = p1;
                match.Player2Id = p2;
            }

            // Funkcja do przypisywania automatycznych zwycięstw w rundach bez przeciwnika
            void AdvanceByes()
            {
                // Iterujemy po rundach od 1 do totalRounds - 1
                for (int round = 1; round < totalRounds; round++)
                {
                    int matchesInRound = (int)Math.Pow(2, totalRounds - round);
                    for (int i = 0; i < matchesInRound; i++)
                    {
                        var match = matches[(round, i)];

                        // Jeśli mecz ma dokładnie jednego gracza i brak zwycięzcy
                        bool hasP1 = !string.IsNullOrEmpty(match.Player1Id);
                        bool hasP2 = !string.IsNullOrEmpty(match.Player2Id);
                        bool hasWinner = !string.IsNullOrEmpty(match.WinnerId);

                        if (hasWinner)
                            continue;

                        if (hasP1 ^ hasP2) // dokładnie jeden gracz
                        {
                            string winner = hasP1 ? match.Player1Id! : match.Player2Id!;
                            match.WinnerId = winner;

                            // Przypisz zwycięzcę do kolejnej rundy
                            int parentRound = round + 1;
                            int parentIndex = i / 2;
                            var parentMatch = matches[(parentRound, parentIndex)];

                            if (i % 2 == 0) // nawet indeks = Player1 w wyższej rundzie
                            {
                                if (string.IsNullOrEmpty(parentMatch.Player1Id))
                                    parentMatch.Player1Id = winner;
                            }
                            else // nieparzysty indeks = Player2
                            {
                                if (string.IsNullOrEmpty(parentMatch.Player2Id))
                                    parentMatch.Player2Id = winner;
                            }
                        }
                    }
                }
            }

            AdvanceByes();

            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group(tournament.Id.ToString()).SendAsync("tournamentStatusUpdated", tournament.Id);
            return true;
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateLadder(int id)
        {
            var success = await TryGenerateLadder(id);

            if (success)
                TempData["Success"] = "Drabinka została wygenerowana.";
            else
            {
                var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.Id == id);

                var players = tournament.Participants.OrderBy(p => p.Ranking).ToList();
                if (players.Count < 2)
                {
                    TempData["ErrorMessage"] = "Zbyt mała liczba uczestników - turniej anlulowany";
                }
                else
                    TempData["ErrorMessage"] = "Nie można wygenerować drabinki.";
            }
                
            return RedirectToAction("Details", new { id });
        }


        // GET: /Tournament/Matches/5
        public async Task<IActionResult> Matches(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                    .ThenInclude(p => p.User)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            // Gracze i mecz po rundach
            var matchesByRound = tournament.Matches
                .OrderBy(m => m.Round)
                .GroupBy(m => m.Round)
                .ToList();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var model = new MatchesViewModel
            {
                Tournament = tournament,
                MatchesByRound = matchesByRound,
                CurrentUserId = userId
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitMatchResult(int matchId, string winnerId)
        {
            var match = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (userId != match.Player1Id && userId != match.Player2Id)
                return Forbid();

            if (winnerId != match.Player1Id && winnerId != match.Player2Id)
                return BadRequest("Wybrany zwycięzca nie uczestniczy w tym meczu.");

            var existingSubmission = await _context.ResultSubmissions
                .FirstOrDefaultAsync(rs => rs.MatchId == matchId && rs.UserId == userId);

            if (existingSubmission != null)
            {
                existingSubmission.WinnerId = winnerId;
                existingSubmission.SubmittedAt = DateTime.Now;
                _context.ResultSubmissions.Update(existingSubmission);
            }
            else
            {
                _context.ResultSubmissions.Add(new ResultSubmission
                {
                    MatchId = matchId,
                    UserId = userId,
                    WinnerId = winnerId,
                    SubmittedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // Sprawdź czy oba wyniki (od dwóch graczy) są zgodne
            var submissions = await _context.ResultSubmissions
                .Where(rs => rs.MatchId == matchId)
                .ToListAsync();

            var player1Submission = submissions.FirstOrDefault(rs => rs.UserId == match.Player1Id);
            var player2Submission = submissions.FirstOrDefault(rs => rs.UserId == match.Player2Id);

            if (player1Submission != null && player2Submission != null)
            {
                if (player1Submission.WinnerId == player2Submission.WinnerId)
                {
                    // Wynik zatwierdzony
                    match.WinnerId = player1Submission.WinnerId;
                    _context.Matches.Update(match);

                    // Usuwamy zgłoszenia wyników
                    _context.ResultSubmissions.RemoveRange(submissions);

                    // Aktualizuj drabinkę
                    await UpdateTournamentLadderAfterMatch(match.TournamentId);

                    // Sprawdź, czy to był ostatni mecz ostatniej rundy
                    var tournament = await _context.Tournaments
                        .Include(t => t.Matches)
                        .FirstOrDefaultAsync(t => t.Id == match.TournamentId);

                    if (tournament != null)
                    {
                        int maxRound = tournament.Matches.Max(m => m.Round);

                        // Sprawdź, czy wszystkie mecze w ostatniej rundzie mają zwycięzców
                        bool allFinalMatchesResolved = tournament.Matches
                            .Where(m => m.Round == maxRound)
                            .All(m => m.WinnerId != null);

                        if (allFinalMatchesResolved)
                        {
                            // Zmieniamy status turnieju na Finished
                            tournament.Status = TournamentStatus.Finished;

                            // Ustawiamy zwycięzcę całego turnieju (zwycięzca ostatniego meczu)
                            var finalMatch = tournament.Matches
                                .FirstOrDefault(m => m.Round == maxRound);

                            if (finalMatch != null && finalMatch.WinnerId != null)
                            {
                                tournament.Winner = finalMatch.WinnerId;
                            }

                            _context.Tournaments.Update(tournament);
                            await _context.SaveChangesAsync();

                            TempData["Success"] += " Turniej został zakończony.";
                        }
                    }


                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Wynik został zatwierdzony i zapisany.";
                }
                else
                {
                    // Wyniki sprzeczne - kasujemy zgłoszenia
                    _context.ResultSubmissions.RemoveRange(submissions);
                    await _context.SaveChangesAsync();

                    TempData["Warning"] = "Wyniki sprzeczne - głosy wycofane.";
                }
            }
            else
            {
                TempData["Info"] = "Jeden z graczy zagłosował";
            }

            return RedirectToAction("Matches", new { id = match.TournamentId });
        }

        private async Task UpdateTournamentLadderAfterMatch(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return;

            var maxRound = tournament.Matches.Max(m => m.Round);
            var currentRoundMatches = tournament.Matches
                .Where(m => m.Round == maxRound)
                .OrderBy(m => m.Id)
                .ToList();

            if (currentRoundMatches.Any(m => string.IsNullOrEmpty(m.WinnerId)))
                return; // czekamy aż wszystkie mecze zostaną rozstrzygnięte

            var winners = currentRoundMatches
                .Select(m => m.WinnerId)
                .Where(id => id != null)
                .ToList();

            if (winners.Count == 1)
            {
                // Jeden zwycięzca – koniec turnieju
                tournament.Status = TournamentStatus.Finished;
                tournament.Winner = winners.First();
                _context.Tournaments.Update(tournament);
                await _context.SaveChangesAsync();
                return;
            }

            var nextRound = maxRound + 1;
            var nextRoundMatches = tournament.Matches
                .Where(m => m.Round == nextRound)
                .OrderBy(m => m.Id)
                .ToList();

            int requiredMatches = (int)Math.Ceiling(winners.Count / 2.0);

            // Jeśli nie istnieją mecze dla kolejnej rundy – utwórz je
            if (nextRoundMatches.Count < requiredMatches)
            {
                for (int i = 0; i < requiredMatches; i++)
                {
                    var newMatch = new Match
                    {
                        TournamentId = tournament.Id,
                        Round = nextRound
                    };
                    _context.Matches.Add(newMatch);
                    nextRoundMatches.Add(newMatch);
                }
                await _context.SaveChangesAsync();
            }

            // Wypełnij mecze zwycięzcami
            for (int i = 0; i < winners.Count; i++)
            {
                int matchIndex = i / 2;
                var match = nextRoundMatches[matchIndex];

                if (i % 2 == 0)
                    match.Player1Id = winners[i];
                else
                    match.Player2Id = winners[i];

                if (match.Player1Id != null && match.Player2Id == null)
                    match.WinnerId = match.Player1Id;
            }

            _context.Matches.UpdateRange(nextRoundMatches);
            await _context.SaveChangesAsync();
        }


        
        public async Task<IActionResult> Bracket(int id, bool isPartial = false)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                    .ThenInclude(p => p.User)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            var viewModel = new TournamentDetailsViewModel
            {
                Tournament = tournament,
                Participants = tournament.Participants.ToList(),
                Matches = tournament.Matches.ToList()
            };

            if (isPartial)
            {
                // Jeśli to żądanie AJAX, zwróć widok częściowy
                return PartialView("_BracketContent", viewModel);
            }

            return View(viewModel);
        }

       [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportResult(int matchId, string winnerId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var match = await _context.Matches
                .Include(m => m.Tournament)
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null)
            {
                TempData["ErrorMessage"] = "Mecz nie został znaleziony.";
                return RedirectToAction("Bracket", new { id = match.TournamentId });
            }

            // Sprawdź, czy zgłaszający użytkownik jest jednym z graczy w meczu
            if (currentUserId != match.Player1Id && currentUserId != match.Player2Id)
            {
                TempData["ErrorMessage"] = "Nie masz uprawnień do zgłaszania wyniku tego meczu.";
                return RedirectToAction("Bracket", new { id = match.TournamentId });
            }

            // Sprawdź, czy zgłoszony zwycięzca jest jednym z graczy w meczu
            if (winnerId != match.Player1Id && winnerId != match.Player2Id)
            {
                TempData["ErrorMessage"] = "Nieprawidłowy zwycięzca zgłoszony.";
                return RedirectToAction("Bracket", new { id = match.TournamentId });
            }

            if (currentUserId == match.Player1Id)
            {
                match.Player1ReportedWinnerId = winnerId;
            }
            else if (currentUserId == match.Player2Id)
            {
                match.Player2ReportedWinnerId = winnerId;
            }

            // Logika rozstrzygania wyników i niezgodności
            if (!string.IsNullOrEmpty(match.Player1ReportedWinnerId) && !string.IsNullOrEmpty(match.Player2ReportedWinnerId))
            {
                // Obaj gracze zgłosili wynik
                if (match.Player1ReportedWinnerId == match.Player2ReportedWinnerId)
                {
                    // Wyniki są zgodne - ustaw zwycięzcę i usuń komunikat o niezgodności
                    match.WinnerId = match.Player1ReportedWinnerId;
                    match.DiscrepancyMessage = null; // Usuń komunikat o niezgodności, jeśli wcześniej istniał

                    var nextRound = match.Round + 1;
                    var nextRoundMatches = await _context.Matches
                        .Where(m => m.TournamentId == match.TournamentId && m.Round == nextRound)
                        .ToListAsync();

                    // Oblicz numer meczu w kolejnej rundzie, do którego ma trafić zwycięzca
                    var allMatchesInCurrentRound = await _context.Matches
                        .Where(m => m.TournamentId == match.TournamentId && m.Round == match.Round)
                        .OrderBy(m => m.Id)
                        .ToListAsync();

                    int currentMatchIndex = allMatchesInCurrentRound.FindIndex(m => m.Id == match.Id);
                    int targetMatchIndex = currentMatchIndex / 2;

                    // Znajdź mecz w kolejnej rundzie, do którego awansuje zwycięzca
                    var nextMatch = nextRoundMatches
                        .Skip(targetMatchIndex)
                        .FirstOrDefault();

                    if (nextMatch == null)
                    {
                        // Jeśli nie ma już kolejnego meczu – zwycięzca zostaje zwycięzcą całego turnieju
                        var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.Id == match.TournamentId);
                        if (tournament != null)
                        {
                            tournament.Status = TournamentStatus.Finished;
                            tournament.Winner = winnerId; // Ustaw ID zwycięzcy turnieju
                            _context.Tournaments.Update(tournament);
                        }
                    }
                    else
                    {
                        // Dodaj zwycięzcę lewa strona
                        if (currentMatchIndex % 2 == 0)
                        {
                            nextMatch.Player1Id = winnerId;
                        }
                        else // Prawa strona
                        {
                            nextMatch.Player2Id = winnerId;
                        }
                        _context.Matches.Update(nextMatch);
                    }
                }
                else
                {
                    // Wyniki są niezgodne - ustaw komunikat o niezgodności i zresetuj głosy
                    match.WinnerId = null; // Upewnij się, że zwycięzca nie jest ustawiony
                    match.DiscrepancyMessage = $"Sprzeczność - głosy wycofane";
                    
                    // Zresetuj zgłoszenia obu graczy, aby mogli zagłosować ponownie
                    match.Player1ReportedWinnerId = null;
                    match.Player2ReportedWinnerId = null;
                }
            }
            else
            {
                match.DiscrepancyMessage = "Oczekiwanie na drugi głos"; 
            }

            await _context.SaveChangesAsync();

            // Powiadom wszystkich klientów w grupie turnieju za pomocą SignalR
            await _hubContext.Clients.Group(match.Tournament.Id.ToString()).SendAsync("bracketUpdated", match.Tournament.Id);

            return RedirectToAction("Bracket", new { id = match.TournamentId });
        }

    }
}
