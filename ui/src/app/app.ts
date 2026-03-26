import { Component, inject, signal, computed, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AstronautService,
  PersonAstronaut, AstronautDuty, AuditLog, AstronautDutyWithPerson,
  AstronautDutiesResponse, PersonResponse, PeopleResponse, AuditLogsResponse,
  AllDutiesResponse, StatsResponse
} from './astronaut.service';

type AsyncState = 'idle' | 'loading' | 'success' | 'error' | 'not-found';
type Tab = 'search' | 'people' | 'add-person' | 'add-duty' | 'all-duties' | 'logs';

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnDestroy {
  private readonly svc = inject(AstronautService);

  // ── Live clock ────────────────────────────────────────────────────────────
  private _now = signal(new Date());
  private readonly _clockInterval = setInterval(() => this._now.set(new Date()), 1000);

  ngOnDestroy() { clearInterval(this._clockInterval); }

  get clockTime(): string {
    return this._now().toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }
  get clockDate(): string {
    return this._now().toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  activeTab = signal<Tab>('search');

  // ── Edit mode / authentication ────────────────────────────────────────────
  editMode = signal(false);
  showLoginDialog = signal(false);
  loginUsername = signal('');
  loginPassword = signal('');
  loginError = signal('');

  toggleEditMode() {
    if (this.editMode()) {
      // Switching back to view mode — no credentials needed
      this.editMode.set(false);
      // If on an edit-only tab, switch to search
      const tab = this.activeTab();
      if (tab === 'add-person' || tab === 'add-duty') {
        this.setTab('search');
      }
    } else {
      // Switching to edit mode — prompt for credentials
      this.loginUsername.set('');
      this.loginPassword.set('');
      this.loginError.set('');
      this.showLoginDialog.set(true);
    }
  }

  // SECURITY NOTE: This is a client-side convenience guard only — NOT real authentication.
  // Anyone who inspects the JS bundle can see these credentials and bypass the gate.
  // In production, this would be replaced with a proper auth flow (OAuth 2.0 / JWT) backed
  // by server-side enforcement. The API itself currently has no auth layer, which is also a gap.
  submitLogin() {
    if (this.loginUsername() === 'tonyd' && this.loginPassword() === 'GodMode') {
      this.editMode.set(true);
      this.showLoginDialog.set(false);
      this.loginError.set('');
    } else {
      this.loginError.set('Invalid credentials.');
    }
  }

  cancelLogin() {
    this.showLoginDialog.set(false);
    this.loginError.set('');
  }

  // ── Stats ─────────────────────────────────────────────────────────────────
  stats = signal<StatsResponse | null>(null);
  statsState = signal<AsyncState>('idle');

  loadStats() {
    this.statsState.set('loading');
    this.svc.getStats().subscribe({
      next: (r: StatsResponse) => {
        this.stats.set(r);
        this.statsState.set('success');
      },
      error: () => this.statsState.set('error')
    });
  }

  // ── Search duty by name ───────────────────────────────────────────────────
  searchName = signal('');
  searchState = signal<AsyncState>('idle');
  searchedName = signal('');
  searchPerson = signal<PersonAstronaut | null>(null);
  searchDuties = signal<AstronautDuty[]>([]);
  searchError = signal('');

  isSearchLoading = computed(() => this.searchState() === 'loading');
  isRetired = computed(() => this.searchPerson()?.currentDutyTitle === 'RETIRED');

  // ── Photo upload ────────────────────────────────────────────────────────
  photoUploadState = signal<AsyncState>('idle');
  photoUploadError = signal('');
  private photoUploadTarget = '';

  @ViewChild('peoplePhotoInput') peoplePhotoInput!: ElementRef<HTMLInputElement>;

  triggerPhotoUpload(name: string) {
    this.photoUploadTarget = name;
    this.peoplePhotoInput.nativeElement.click();
  }

  onPhotoSelectedForPerson(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.photoUploadTarget) return;

    this.photoUploadState.set('loading');
    this.photoUploadError.set('');

    this.svc.uploadPhoto(this.photoUploadTarget, file).subscribe({
      next: () => {
        this.photoUploadState.set('success');
        if (this.peopleState() === 'success') this.loadPeople();
        if (this.searchState() === 'success' && this.searchPerson()?.name === this.photoUploadTarget) this.onSearch();
        if (this.lookupState() === 'success' && this.lookupPerson()?.name === this.photoUploadTarget) this.onLookup();
        setTimeout(() => this.photoUploadState.set('idle'), 3000);
      },
      error: (e: HttpErrorResponse) => {
        this.photoUploadError.set(e.error?.message || e.message);
        this.photoUploadState.set('error');
        setTimeout(() => this.photoUploadState.set('idle'), 5000);
      }
    });
    input.value = '';
  }

  // ── Photo lightbox ──────────────────────────────────────────────────────
  lightboxPerson = signal<PersonAstronaut | null>(null);

  openLightbox(person: PersonAstronaut) {
    if (person.photoUrl) this.lightboxPerson.set(person);
  }

  closeLightbox() {
    this.lightboxPerson.set(null);
  }

  onSearch() {
    const name = this.searchName().trim();
    if (!name) return;
    this.searchState.set('loading');
    this.searchedName.set(name);
    this.searchPerson.set(null);
    this.searchDuties.set([]);
    this.searchError.set('');

    this.svc.getAstronautDutiesByName(name).subscribe({
      next: (r: AstronautDutiesResponse) => {
        if (!r.person) { this.searchState.set('not-found'); return; }
        this.searchPerson.set(r.person);
        this.searchDuties.set(r.astronautDuties ?? []);
        this.searchState.set('success');
      },
      error: (e: HttpErrorResponse) => {
        this.searchError.set(e.error?.message || e.message);
        this.searchState.set('error');
      }
    });
  }

  onSearchClear() {
    this.searchName.set('');
    this.searchState.set('idle');
    this.searchPerson.set(null);
    this.searchDuties.set([]);
  }

  // ── All people ────────────────────────────────────────────────────────────
  peopleState = signal<AsyncState>('idle');
  people = signal<PersonAstronaut[]>([]);
  peopleError = signal('');
  peopleFilter = signal('');

  filteredPeople = computed(() => {
    const filter = this.peopleFilter().toLowerCase().trim();
    if (!filter) return this.people();
    return this.people().filter(p =>
      p.name.toLowerCase().includes(filter) ||
      (p.currentDutyTitle ?? '').toLowerCase().includes(filter) ||
      (p.currentRank ?? '').toLowerCase().includes(filter)
    );
  });

  // inline person-lookup inside the People tab
  lookupName = signal('');
  lookupState = signal<AsyncState>('idle');
  lookupPerson = signal<PersonAstronaut | null>(null);
  lookupError = signal('');

  // inline rename
  renamingName = signal('');
  newNameInput = signal('');
  renameState = signal<AsyncState>('idle');
  renameError = signal('');
  renameSuccess = signal('');

  loadPeople() {
    this.peopleState.set('loading');
    this.svc.getAllPeople().subscribe({
      next: (r: PeopleResponse) => {
        this.people.set(r.people ?? []);
        this.peopleState.set('success');
      },
      error: (e: HttpErrorResponse) => {
        this.peopleError.set(e.error?.message || e.message);
        this.peopleState.set('error');
      }
    });
  }

  onLookup() {
    const name = this.lookupName().trim();
    if (!name) return;
    this.lookupState.set('loading');
    this.lookupPerson.set(null);
    this.lookupError.set('');
    this.svc.getPersonByName(name).subscribe({
      next: (r: PersonResponse) => {
        if (!r.person) { this.lookupState.set('not-found'); return; }
        this.lookupPerson.set(r.person);
        this.lookupState.set('success');
      },
      error: (e: HttpErrorResponse) => {
        this.lookupError.set(e.error?.message || e.message);
        this.lookupState.set('error');
      }
    });
  }

  startRename(name: string) {
    this.renamingName.set(name);
    this.newNameInput.set(name);
    this.renameState.set('idle');
    this.renameError.set('');
    this.renameSuccess.set('');
  }

  cancelRename() { this.renamingName.set(''); }

  submitRename() {
    const current = this.renamingName().trim();
    const next = this.newNameInput().trim();
    if (!current || !next || current === next) return;
    this.renameState.set('loading');
    this.renameError.set('');
    this.renameSuccess.set('');
    this.svc.updatePerson(current, next).subscribe({
      next: () => {
        this.renameSuccess.set(`Renamed "${current}" to "${next}".`);
        this.renameState.set('success');
        this.renamingName.set('');
        this.loadPeople();
        this.loadStats();
        if (this.lookupPerson()?.name === current) this.onLookup();
      },
      error: (e: HttpErrorResponse) => {
        this.renameError.set(e.error?.message || e.message);
        this.renameState.set('error');
      }
    });
  }

  viewPersonDuties(name: string) {
    this.searchName.set(name);
    this.activeTab.set('search');
    setTimeout(() => this.onSearch(), 0);
  }

  // ── Add person ────────────────────────────────────────────────────────────
  addPersonName = signal('');
  addPersonState = signal<AsyncState>('idle');
  addPersonError = signal('');
  addPersonSuccess = signal('');

  submitAddPerson() {
    const name = this.addPersonName().trim();
    if (!name) return;
    this.addPersonState.set('loading');
    this.addPersonError.set('');
    this.addPersonSuccess.set('');
    this.svc.addPerson(name).subscribe({
      next: (r) => {
        this.addPersonSuccess.set(`"${name}" added (ID ${r.id}).`);
        this.addPersonState.set('success');
        this.addPersonName.set('');
        this.loadStats();
      },
      error: (e: HttpErrorResponse) => {
        this.addPersonError.set(e.error?.message || e.message);
        this.addPersonState.set('error');
      }
    });
  }

  // ── Add duty ──────────────────────────────────────────────────────────────
  dutyName = signal('');
  dutyRank = signal('');
  dutyTitle = signal('');
  dutyStartDate = signal('');
  addDutyState = signal<AsyncState>('idle');
  addDutyError = signal('');
  addDutySuccess = signal('');

  submitAddDuty() {
    const name = this.dutyName().trim();
    const rank = this.dutyRank().trim();
    const title = this.dutyTitle().trim();
    const date = this.dutyStartDate();
    if (!name || !rank || !title || !date) return;
    this.addDutyState.set('loading');
    this.addDutyError.set('');
    this.addDutySuccess.set('');
    this.svc.addAstronautDuty({ name, rank, dutyTitle: title, dutyStartDate: date }).subscribe({
      next: (r) => {
        this.addDutySuccess.set(`Duty added (ID ${r.id}).`);
        this.addDutyState.set('success');
        this.dutyName.set('');
        this.dutyRank.set('');
        this.dutyTitle.set('');
        this.dutyStartDate.set('');
        this.loadStats();
      },
      error: (e: HttpErrorResponse) => {
        this.addDutyError.set(e.error?.message || e.message);
        this.addDutyState.set('error');
      }
    });
  }

  // ── All duties ────────────────────────────────────────────────────────────
  allDutiesState = signal<AsyncState>('idle');
  allDuties = signal<AstronautDutyWithPerson[]>([]);
  allDutiesError = signal('');
  allDutiesFilter = signal('');
  allDutiesSortCol = signal<'personName' | 'dutyTitle' | 'dutyStartDate'>('dutyStartDate');
  allDutiesSortAsc = signal(false);

  filteredAllDuties = computed(() => {
    const filter = this.allDutiesFilter().toLowerCase().trim();
    let duties = filter
      ? this.allDuties().filter(d =>
          d.personName.toLowerCase().includes(filter) ||
          d.dutyTitle.toLowerCase().includes(filter) ||
          d.rank.toLowerCase().includes(filter)
        )
      : this.allDuties();

    const col = this.allDutiesSortCol();
    const asc = this.allDutiesSortAsc();
    return [...duties].sort((a, b) => {
      const av = a[col] ?? '';
      const bv = b[col] ?? '';
      return asc ? (av < bv ? -1 : av > bv ? 1 : 0) : (av > bv ? -1 : av < bv ? 1 : 0);
    });
  });

  loadAllDuties() {
    this.allDutiesState.set('loading');
    this.svc.getAllAstronautDuties().subscribe({
      next: (r: AllDutiesResponse) => {
        this.allDuties.set(r.astronautDuties ?? []);
        this.allDutiesState.set('success');
      },
      error: (e: HttpErrorResponse) => {
        this.allDutiesError.set(e.error?.message || e.message);
        this.allDutiesState.set('error');
      }
    });
  }

  sortAllDuties(col: 'personName' | 'dutyTitle' | 'dutyStartDate') {
    if (this.allDutiesSortCol() === col) {
      this.allDutiesSortAsc.update(v => !v);
    } else {
      this.allDutiesSortCol.set(col);
      this.allDutiesSortAsc.set(col !== 'dutyStartDate');
    }
  }

  sortIcon(col: string): string {
    if (this.allDutiesSortCol() !== col) return '↕';
    return this.allDutiesSortAsc() ? '↑' : '↓';
  }

  // ── Edit duty ─────────────────────────────────────────────────────────────
  editingDutyId = signal<number | null>(null);
  editDutyRank = signal('');
  editDutyTitle = signal('');
  editDutyStartDate = signal('');
  editDutyEndDate = signal('');
  editDutyState = signal<AsyncState>('idle');
  editDutyError = signal('');
  editDutySuccess = signal('');

  startEditDuty(duty: AstronautDuty | AstronautDutyWithPerson) {
    this.editingDutyId.set(duty.id);
    this.editDutyRank.set(duty.rank);
    this.editDutyTitle.set(duty.dutyTitle);
    this.editDutyStartDate.set(duty.dutyStartDate ? duty.dutyStartDate.substring(0, 10) : '');
    this.editDutyEndDate.set(duty.dutyEndDate ? duty.dutyEndDate.substring(0, 10) : '');
    this.editDutyState.set('idle');
    this.editDutyError.set('');
    this.editDutySuccess.set('');
  }

  cancelEditDuty() { this.editingDutyId.set(null); }

  submitEditDuty() {
    const id = this.editingDutyId();
    const rank = this.editDutyRank().trim();
    const title = this.editDutyTitle().trim();
    const startDate = this.editDutyStartDate();
    if (!id || !rank || !title || !startDate) return;

    this.editDutyState.set('loading');
    this.editDutyError.set('');
    this.editDutySuccess.set('');

    this.svc.updateAstronautDuty(id, {
      rank,
      dutyTitle: title,
      dutyStartDate: startDate,
      dutyEndDate: this.editDutyEndDate() || null
    }).subscribe({
      next: () => {
        this.editDutySuccess.set('Duty updated successfully.');
        this.editDutyState.set('success');
        this.editingDutyId.set(null);
        // Refresh all relevant data
        if (this.searchState() === 'success') this.onSearch();
        if (this.allDutiesState() === 'success') this.loadAllDuties();
        this.loadStats();
      },
      error: (e: HttpErrorResponse) => {
        this.editDutyError.set(e.error?.message || e.message);
        this.editDutyState.set('error');
      }
    });
  }

  // ── Logs ─────────────────────────────────────────────────────────────────
  logsState = signal<AsyncState>('idle');
  logs = signal<AuditLog[]>([]);
  logsError = signal('');
  logsTotalCount = signal(0);
  logsPage = signal(1);
  readonly logsPageSize = 50;
  logsSearch = signal('');
  logsSortBy = signal<'date' | 'status' | 'message'>('date');
  logsSortDirection = signal<'asc' | 'desc'>('desc');

  loadLogs(page = 1) {
    this.logsState.set('loading');
    this.logsPage.set(page);
    const search = this.logsSearch().trim() || undefined;
    this.svc.getAuditLogs(page, this.logsPageSize, search, this.logsSortBy(), this.logsSortDirection()).subscribe({
      next: (r: AuditLogsResponse) => {
        this.logs.set(r.logs ?? []);
        this.logsTotalCount.set(r.totalCount);
        this.logsState.set('success');
      },
      error: (e: HttpErrorResponse) => {
        this.logsError.set(e.error?.message || e.message);
        this.logsState.set('error');
      }
    });
  }

  onLogsSearch(value: string) {
    this.logsSearch.set(value);
    this.loadLogs(1);
  }

  toggleLogsSort(column: 'date' | 'status' | 'message') {
    if (this.logsSortBy() === column) {
      this.logsSortDirection.set(this.logsSortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.logsSortBy.set(column);
      this.logsSortDirection.set(column === 'date' ? 'desc' : 'asc');
    }
    this.loadLogs(1);
  }

  get logsTotalPages(): number {
    return Math.ceil(this.logsTotalCount() / this.logsPageSize);
  }

  // ── Navigation ────────────────────────────────────────────────────────────
  setTab(tab: Tab) {
    this.activeTab.set(tab);
    if (tab === 'people') this.loadPeople();
    if (tab === 'all-duties') this.loadAllDuties();
    if (tab === 'logs') this.loadLogs(1);
    if (this.statsState() === 'idle') this.loadStats();
  }

  // ── Shared helpers ────────────────────────────────────────────────────────
  onKeyDown(event: KeyboardEvent, action: () => void) {
    if (event.key === 'Enter') action();
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatDateTime(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false
    });
  }

  toDateInputValue(d: string | null): string {
    if (!d) return '';
    return d.substring(0, 10);
  }

  splitLogMessage(msg: string): { action: string; params: string | null } {
    const idx = msg.indexOf(' | ');
    if (idx === -1) return { action: msg, params: null };
    return { action: msg.slice(0, idx), params: msg.slice(idx + 3) };
  }

  isCurrentDuty(duty: AstronautDuty | AstronautDutyWithPerson): boolean {
    return duty.dutyEndDate === null;
  }

  get careerDuration(): string {
    const p = this.searchPerson();
    if (!p?.careerStartDate) return '';
    const start = new Date(p.careerStartDate);
    const end = p.careerEndDate ? new Date(p.careerEndDate) : new Date();
    const years = Math.floor((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24 * 365.25));
    return years === 1 ? '1 year' : `${years} years`;
  }

  // ── Delete person ─────────────────────────────────────────────────────────
  deletePersonState = signal<AsyncState>('idle');

  confirmDeletePerson(name: string) {
    if (!confirm(`Delete "${name}" and all their duties? This cannot be undone.`)) return;
    this.deletePersonState.set('loading');
    this.svc.deletePerson(name).subscribe({
      next: () => {
        this.deletePersonState.set('success');
        this.loadPeople();
        this.loadStats();
        if (this.lookupPerson()?.name === name) { this.lookupState.set('idle'); this.lookupPerson.set(null); }
        if (this.searchPerson()?.name === name) { this.searchState.set('idle'); this.searchPerson.set(null); }
      },
      error: (e: HttpErrorResponse) => {
        this.deletePersonState.set('error');
        alert(e.error?.message || e.message);
      }
    });
  }

  // ── Delete duty ───────────────────────────────────────────────────────────
  confirmDeleteDuty(dutyId: number) {
    if (!confirm(`Delete duty #${dutyId}? This cannot be undone.`)) return;
    this.svc.deleteDuty(dutyId).subscribe({
      next: () => {
        if (this.searchState() === 'success') this.onSearch();
        if (this.allDutiesState() === 'success') this.loadAllDuties();
        this.loadStats();
      },
      error: (e: HttpErrorResponse) => {
        alert(e.error?.message || e.message);
      }
    });
  }
}
