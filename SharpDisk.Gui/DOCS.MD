# SharpDisk.Gui — jak to działa

Notatka dla mnie z przyszłości. Opisuje, z czego złożone jest GUI, gdzie co leży i dlaczego
zostało zrobione tak, a nie inaczej.

---

## 1. Uruchamianie

```bash
dotnet build SharpDisk.Gui/SharpDisk.Gui.csproj
./SharpDisk.Gui/bin/Debug/net10.0/SharpDisk.Gui                 # startuje na liście dysków
./SharpDisk.Gui/bin/Debug/net10.0/SharpDisk.Gui /sciezka/do.iso # otwiera od razu
./SharpDisk.Gui/bin/Debug/net10.0/SharpDisk.Gui /dev/sda        # to samo, ale urządzenie
```

Pierwszy argument niebędący flagą jest traktowany jako ścieżka do otwarcia
(`Services/StartupOptions.cs`). Przydaje się przy powtarzalnym testowaniu jednego obrazu.

Odczyt `/dev/*` wymaga roota — patrz sekcja 8.

---

## 2. Stos

| Warstwa | Co | Gdzie |
|---|---|---|
| Okno | Photino (WebKitGTK na Linuksie) | `third-party/Photino.Blazor` (zwendorowane, Apache-2.0) |
| UI | Blazor Hybrid, routing przez `<Router>` | `App.razor` |
| Style | MDB UI Kit 9.3.0 (Material Design for Bootstrap 5), edycja free, MIT | `wwwroot/lib/mdb` |
| Logika dysków | `SharpDisk.Core` + `SharpDisk.Lib` | referencje projektowe |

**MDB jest zwendorowany, nie z CDN-u.** To aplikacja desktopowa, która ma działać bez sieci.
`mdb.min.css` zawiera już w sobie Bootstrapa, więc osobny Bootstrap nie jest potrzebny.
Ikony: brak Font Awesome — dwa potrzebne glify są wklejone jako inline SVG w `Layout/NavMenu.razor`.
Aktualizacja MDB: podmienić dwa pliki z paczki npm `mdb-ui-kit`, zaktualizować `wwwroot/lib/mdb/README.md`.

**Nie nadpisuj styli MDB w `app.css`.** `app.css` zawiera tylko to, czego MDB nie ma: szkielet okna
(sidebar + treść) i mapę partycji. Karty, tabele, badge, przyciski i alerty biorą klasy MDB
bezpośrednio w markupie.

### Motyw ciemny

Włączany jednym atrybutem: `<html lang="pl" data-mdb-theme="dark">` w `index.html`. MDB ma pełny
zestaw tokenów pod tym selektorem (`--mdb-body-bg: #303030`, `--mdb-surface-bg: #424242` dla kart,
`--mdb-border-color: rgba(255,255,255,.12)`, plus `color-scheme: dark`, dzięki czemu natywne
scrollbary i inputy też są ciemne).

Zmienne `--sd-*` w `app.css` **wiszą na zmiennych MDB**, gdzie tylko taka istnieje
(`--sd-surface: var(--mdb-surface-bg)` itd.), więc oba motywy nie mogą się rozjechać. Własne
wartości mają tylko rzeczy, których MDB nie zna: sidebar (`#1f1f1f`, celowo ciemniejszy niż tło
treści, żeby panele się rozdzielały) i dwa szarości kreskowania pasów.

Dwie rzeczy, o których trzeba pamiętać przy dokładaniu markupu:

- **`bg-white` i `bg-dark` są wypalone na sztywno** i nie reagują na motyw. `bg-white` zrobi białą
  belkę w środku ciemnej karty, `bg-dark` badge zniknie. Używaj domyślnego tła karty albo
  `bg-secondary`.
- **Paleta partycji jest dobrana pod ciemne tło** — odcienie Material 300–400, nie 600. Niebieski
  600-ki na pasie `#2c2c2c` praktycznie nie odróżnia się od tła. Wypełnienie segmentu ma
  `opacity: .26` zamiast `.17` z tego samego powodu.

---

## 3. Architektura

Wszystko idzie przez DI zarejestrowane w `Program.cs`. Strony nie dotykają `SharpDisk.Lib`
bezpośrednio — zawsze przez interfejs serwisu.

```
Program.cs
 ├─ IBlockDeviceService   → BlockDeviceService     listowanie dysków, otwieranie urządzenia
 ├─ IDiskOpenService      → DiskOpenService        ścieżka/urządzenie → OpenedDisk
 ├─ IDiskMapBuilder       → DiskMapBuilder         tablica partycji → DiskMap (bloki do narysowania)
 ├─ IAnalysisReportBuilder→ AnalysisReportBuilder  bitmaska analizatora → lista komunikatów
 ├─ IFileDialogService    → PhotinoFileDialogService   natywny file picker
 ├─ DiskSession                                    aktualnie otwarty nośnik (stan współdzielony)
 └─ StartupOptions                                 argumenty z wiersza poleceń
```

Wszystko jako **singleton** — to aplikacja jednookienkowa, nie ma zakresu żądania, a `DiskSession`
musi być współdzielona, bo inaczej lista dysków i podgląd miałyby własne kopie.

### Przepływ

```
DriveListPage / OpenSourcePage
        │  IDiskOpenService.OpenDeviceAsync / OpenPathAsync
        ▼
  DriveStream (BlockDeviceStream albo FileDriveStream)
        │  ReadSectionAsync(0, 512)
        ▼
  MbrManager.CreateFromBytes(...) + .Analyze()
        ▼
  OpenedDisk  ──►  DiskSession.Open(...)  ──►  NavigateTo("disk")
        ▼
  DiskPage: IDiskMapBuilder + IAnalysisReportBuilder → PartitionMap + AnalysisReport
```

`OpenedDisk` to **migawka, nie uchwyt** — strumień jest czytany i zamykany podczas otwierania, więc
nic nie trzyma deskryptora na urządzeniu. Jak dojdzie zapis, trzeba będzie otworzyć ponownie
(do zapisu i prawdopodobnie z blokadą).

`DiskSession` woła zdarzenie `Changed`. Komponenty subskrybują w `OnInitialized`, odsubskrybowują
w `Dispose`, a przerysowanie robią przez `InvokeAsync(StateHasChanged)`, bo handler może przyjść
spoza wątku UI.

---

## 4. Struktura katalogów

```
Models/       obiekty bez logiki: DiskSource, OpenedDisk, DiskMap/DiskSegment, AnalysisMessage
Services/     cała logika, każdy publiczny serwis za interfejsem
Layout/       MainLayout (szkielet) + NavMenu (sidebar)
Pages/        DriveListPage "/", OpenSourcePage "/open", DiskPage "/disk"
Components/   PartitionMap, AnalysisReport, PageHeader, Fact, ErrorAlert, SeverityBadge
wwwroot/      index.html, css/app.css, lib/mdb/
```

---

## 5. Mapa partycji

To jest ten „śmieszny graf" z GPartedu. Rysuje go `Components/PartitionMap.razor`, a liczy
`Services/DiskMapBuilder.cs`. Podział pracy: **builder liczy, komponent tylko rysuje.**

### Pasy (lanes)

Zwykły rysunek od lewej do prawej się tu nie sprawdza z dwóch powodów:

1. **MBR pozwala partycjom nachodzić na siebie.** Nic w formacie tego nie zabrania, a uszkodzona
   tablica robi to regularnie. Dlatego partycje są pakowane w **pasy**: każda trafia do pierwszego
   wiersza, w którym nie koliduje z tym, co już tam jest. Jeden wiersz = zdrowy dysk. Drugi wiersz
   = nakładanie, które widać, zamiast o nim czytać.

2. **Wpis 0xEE w hybrid MBR z założenia obejmuje cały nośnik.** Gdyby go rysować inline, wypchnąłby
   każdą prawdziwą partycję do własnego wiersza. Dlatego wpisy protective dostają własne, cienkie
   pasy **nad** mapą — rysowane jak parasol, nie jak blok.

`DiskMap.FirstDataLane` mówi, od którego pasa zaczynają się prawdziwe partycje. `HasOverlap`
porównuje się właśnie z nim — inaczej każdy hybrid MBR fałszywie raportowałby nakładanie.

### Pozycjonowanie

Segmenty są pozycjonowane absolutnie, `left` i `width` w **procentach całego dysku**, liczone
w builderze jako `StartFraction` / `WidthFraction`. Proporcje zostają prawdziwe niezależnie od
szerokości okna, a `min-width: 4px` w CSS pilnuje, żeby partycja 2048-sektorowa na dysku 2 TB
nie zniknęła zupełnie.

> **Uwaga na kulturę.** Procenty w atrybucie `style` muszą mieć kropkę. Aplikacja chodzi na polskiej
> kulturze, gdzie `ToString()` zrobiłby `12,5`, co przeglądarka po cichu zignoruje. Dlatego
> `PartitionMap.Percent()` jawnie używa `CultureInfo.InvariantCulture`.

### Wolne miejsce

Liczone **tylko na pierwszym pasie danych** — na pasach nakładania dziura nie znaczy nic. Kreskowane
tło pasa jest tym, co widać w miejscach, gdzie nic nie narysowano; pasy nakładania mają zamiast tego
tło białe z kreskowaną ramką, żeby nie czytały się jak wolne miejsce.

Luka zaczyna się od LBA 0, więc sektor MBR jest częścią pierwszej luki. Tak jest uczciwiej niż
udawać, że dysk zaczyna się od LBA 1.

### Kolory

`Services/PartitionPalette.cs`. Rodziny dzielą odcień jak w GPartedzie: FAT zielony, NTFS morski,
Linux niebieski, swap fioletowy, EFI bursztynowy, extended brązowy, 0xEE ciemny szaroniebieski.
Typy bez opinii lecą w tablicę zapasową indeksowaną bajtem typu — brzydkie, ale stabilne między
uruchomieniami.

Czerwona obwódka segmentu leci **tylko przy `Severity.Error`**. Ostrzeżenia w stylu
`ChsLbaMismatch` zapalają się praktycznie na każdym wpisie i obwódka na wszystkim byłaby szumem.

---

## 6. Analiza

`AnalysisReportBuilder` rozwija bitmaski z `MbrAnalyzeResult` na listę `AnalysisMessage`.
**Cały tekst pochodzi z wygenerowanych klas opisów**, czyli z atrybutów `[ErrorDescription]`
na `MbrErrors` i `MbrPartitionErrors`:

```csharp
MbrErrorDescriptions.Describe(result.TableErrors)              // flagi tablicy
MbrPartitionErrorDescriptions.Describe(result.Partition1Errors) // flagi wpisu 1
MbrPartitionErrorDescriptions.MaxSeverity(errors)               // najgorsza severity
```

Z tego wynika rzecz, o której warto pamiętać: **żeby dodać nową diagnozę, wystarczy dopisać flagę
do enuma, dać jej atrybut i ustawić ją w analizatorze.** W GUI nie ma żadnej listy do zaktualizowania.
To samo dotyczy nazw typów partycji — `MbrPartitionTypeDescriptions.GetPrimary(byte)`.

---

## 7. i18n

Chrome interfejsu (menu, nagłówki, komunikaty GUI) jest po polsku na sztywno w plikach `.razor`.
Opisy z Core lecą po angielsku, dopóki nie podłoży się tłumaczeń — i to jest już zrobione po stronie
biblioteki, tylko nieużyte.

Żeby włączyć polskie opisy:

1. Dodać `Microsoft.Extensions.Localization` do `SharpDisk.Gui`.
2. Wrzucić `SharpDisk.Core/Resources/Localization.SharpDiskDescriptions.pl.resx`, gdzie
   `data name` to **angielski tekst z atrybutu** (np. `Linux Filesystem`), a `value` to tłumaczenie.
3. W `Program.cs`:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
// po zbudowaniu app:
SharpDiskLocalization.Localizer = app.Services.GetRequiredService<IStringLocalizer<SharpDiskDescriptions>>();
CultureInfo.CurrentUICulture = new CultureInfo("pl");
```

`AddLocalization` wymaga `AddLogging()` w kontenerze — już jest. Nieprzetłumaczone stringi
spadają na angielski, więc częściowe tłumaczenie niczego nie psuje. Komplet kluczy do przetłumaczenia
to `MbrPartitionTypeDescriptions.All.Select(i => i.TypeInvariant)` i analogicznie dla opisów i błędów.

---

## 8. Uprawnienia

Listowanie dysków czyta `/sys/class/block` i działa bez roota. **Odczyt sektora 0 z `/dev/*` już nie.**

```bash
scripts/run-gui-root.sh
```

Skrypt wybiera `pkexec`, jeśli chodzi agent polkit, inaczej `sudo` z promptem zenity/yad, a w
ostateczności pyta w terminalu. Przy braku uprawnień obie strony (lista dysków i otwieranie ścieżki)
łapią `UnauthorizedAccessException` i pokazują podpowiedź o tym skrypcie.

Ciekawostka z tej maszyny: `/dev/sda` da się czytać bez roota, bo reguła udev od OpenRGB taguje
obudowę USB↔NVMe (`0bda:9210`) jako `uaccess` i dokleja ACL dla zalogowanego użytkownika. NVMe
wewnętrzne takiego ACL nie mają.

---

## 9. Jak dodać rzeczy

**Nową stronę:** plik w `Pages/` z `@page "/cos"`, wpis `<NavLink>` w `Layout/NavMenu.razor`.
Router bierze strony z assembly automatycznie.

**Nowy serwis:** interfejs + implementacja w `Services/`, rejestracja w `Program.cs`, wstrzyknięcie
przez `@inject`. Nie wołaj `SharpDisk.Lib` ze strony — to jedyna zasada, której warto tu pilnować.

**Nowe pole na mapie:** dołóż property do `DiskSegment`, wypełnij w `DiskMapBuilder.Describe`,
użyj w `PartitionMap.razor`. Komponent nie liczy niczego sam.

---

## 10. Testowanie bez klikania

Serwisy są zwykłymi klasami — da się je odpalić z konsoli bez okna. Mały projekt z referencją do
`SharpDisk.Gui.csproj` i:

```csharp
var devices = new BlockDeviceService(NullLogger<BlockDeviceService>.Instance);
var opener  = new DiskOpenService(devices, NullLogger<DiskOpenService>.Instance);
var disk    = await opener.OpenPathAsync(path);
var map     = new DiskMapBuilder().Build(disk);
var report  = new AnalysisReportBuilder().Build(disk.Analysis);
```

Obraz testowy z prawdziwym hybrid MBR (0xEE + trzy partycje, jedna celowo niewyrównana) robi się
skryptem — 64 MiB rzadkiego pliku, ręcznie sklecone 4 wpisy po 16 bajtów, `55 AA` na końcu.
Weryfikacja: `fdisk -l plik.img`.

Do podejrzenia wyglądu bez klikania: argument ze ścieżką + zrzut okna po nazwie
(`xprop -root _NET_CLIENT_LIST`, potem `import -window <id>`).

---

## 11. Czego jeszcze nie ma

- **Zapisu.** GUI jest w tej chwili wyłącznie do odczytu. Metody mutujące w `MbrManager` to nadal
  stuby. Uwaga przy podłączaniu: `MbrPartitionTable.ToBinary()` zeruje bajty 0–445, więc zapisanie
  tego na dysk skasowałoby bootloader i sygnaturę — `MbrManager.ToBinary()` musi nałożyć tablicę
  na kopię `OriginalMbrData`.
- **GPT.** Czysty protective MBR pokazuje tylko komunikat, bo `SharpDisk.Core` nie ma jeszcze logiki GPT
  poza listą GUID-ów typów.
- **EBR / partycji logicznych.** Wpis typu 0x05/0x0F jest rysowany jako jeden blok; tego, co siedzi
  w środku, nikt nie czyta.
- **Podglądu partycji z listy dysków.** Lista pokazuje liczbę partycji i ile jest zamontowanych,
  ale nie rozwija drzewa.
