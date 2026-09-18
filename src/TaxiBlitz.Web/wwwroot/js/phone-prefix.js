/* phone-prefix.js — international dialling code selector */
var PHONE_PREFIXES = [
    { label: "Macedonia (+389)", prefix: "+389" },
    { label: "Afghanistan (+93)", prefix: "+93" },
    { label: "Albania (+355)", prefix: "+355" },
    { label: "Algeria (+213)", prefix: "+213" },
    { label: "Andorra (+376)", prefix: "+376" },
    { label: "Angola (+244)", prefix: "+244" },
    { label: "Argentina (+54)", prefix: "+54" },
    { label: "Armenia (+374)", prefix: "+374" },
    { label: "Australia (+61)", prefix: "+61" },
    { label: "Austria (+43)", prefix: "+43" },
    { label: "Azerbaijan (+994)", prefix: "+994" },
    { label: "Bahrain (+973)", prefix: "+973" },
    { label: "Bangladesh (+880)", prefix: "+880" },
    { label: "Belarus (+375)", prefix: "+375" },
    { label: "Belgium (+32)", prefix: "+32" },
    { label: "Belize (+501)", prefix: "+501" },
    { label: "Benin (+229)", prefix: "+229" },
    { label: "Bhutan (+975)", prefix: "+975" },
    { label: "Bolivia (+591)", prefix: "+591" },
    { label: "Bosnia and Herzegovina (+387)", prefix: "+387" },
    { label: "Botswana (+267)", prefix: "+267" },
    { label: "Brazil (+55)", prefix: "+55" },
    { label: "Brunei (+673)", prefix: "+673" },
    { label: "Bulgaria (+359)", prefix: "+359" },
    { label: "Burkina Faso (+226)", prefix: "+226" },
    { label: "Burundi (+257)", prefix: "+257" },
    { label: "Cambodia (+855)", prefix: "+855" },
    { label: "Cameroon (+237)", prefix: "+237" },
    { label: "Canada (+1)", prefix: "+1" },
    { label: "Central African Republic (+236)", prefix: "+236" },
    { label: "Chad (+235)", prefix: "+235" },
    { label: "Chile (+56)", prefix: "+56" },
    { label: "China (+86)", prefix: "+86" },
    { label: "Colombia (+57)", prefix: "+57" },
    { label: "Comoros (+269)", prefix: "+269" },
    { label: "Congo (+242)", prefix: "+242" },
    { label: "Congo DRC (+243)", prefix: "+243" },
    { label: "Costa Rica (+506)", prefix: "+506" },
    { label: "Croatia (+385)", prefix: "+385" },
    { label: "Cuba (+53)", prefix: "+53" },
    { label: "Cyprus (+357)", prefix: "+357" },
    { label: "Czech Republic (+420)", prefix: "+420" },
    { label: "Denmark (+45)", prefix: "+45" },
    { label: "Djibouti (+253)", prefix: "+253" },
    { label: "Dominican Republic (+1-809)", prefix: "+1-809" },
    { label: "Ecuador (+593)", prefix: "+593" },
    { label: "Egypt (+20)", prefix: "+20" },
    { label: "El Salvador (+503)", prefix: "+503" },
    { label: "Eritrea (+291)", prefix: "+291" },
    { label: "Estonia (+372)", prefix: "+372" },
    { label: "Ethiopia (+251)", prefix: "+251" },
    { label: "Fiji (+679)", prefix: "+679" },
    { label: "Finland (+358)", prefix: "+358" },
    { label: "France (+33)", prefix: "+33" },
    { label: "Gabon (+241)", prefix: "+241" },
    { label: "Gambia (+220)", prefix: "+220" },
    { label: "Georgia (+995)", prefix: "+995" },
    { label: "Germany (+49)", prefix: "+49" },
    { label: "Ghana (+233)", prefix: "+233" },
    { label: "Greece (+30)", prefix: "+30" },
    { label: "Guatemala (+502)", prefix: "+502" },
    { label: "Guinea (+224)", prefix: "+224" },
    { label: "Guyana (+592)", prefix: "+592" },
    { label: "Haiti (+509)", prefix: "+509" },
    { label: "Honduras (+504)", prefix: "+504" },
    { label: "Hungary (+36)", prefix: "+36" },
    { label: "Iceland (+354)", prefix: "+354" },
    { label: "India (+91)", prefix: "+91" },
    { label: "Indonesia (+62)", prefix: "+62" },
    { label: "Iran (+98)", prefix: "+98" },
    { label: "Iraq (+964)", prefix: "+964" },
    { label: "Ireland (+353)", prefix: "+353" },
    { label: "Israel (+972)", prefix: "+972" },
    { label: "Italy (+39)", prefix: "+39" },
    { label: "Jamaica (+1-876)", prefix: "+1-876" },
    { label: "Japan (+81)", prefix: "+81" },
    { label: "Jordan (+962)", prefix: "+962" },
    { label: "Kazakhstan (+7)", prefix: "+7" },
    { label: "Kenya (+254)", prefix: "+254" },
    { label: "Kosovo (+383)", prefix: "+383" },
    { label: "Kuwait (+965)", prefix: "+965" },
    { label: "Kyrgyzstan (+996)", prefix: "+996" },
    { label: "Laos (+856)", prefix: "+856" },
    { label: "Latvia (+371)", prefix: "+371" },
    { label: "Lebanon (+961)", prefix: "+961" },
    { label: "Lesotho (+266)", prefix: "+266" },
    { label: "Liberia (+231)", prefix: "+231" },
    { label: "Libya (+218)", prefix: "+218" },
    { label: "Liechtenstein (+423)", prefix: "+423" },
    { label: "Lithuania (+370)", prefix: "+370" },
    { label: "Luxembourg (+352)", prefix: "+352" },
    { label: "Madagascar (+261)", prefix: "+261" },
    { label: "Malawi (+265)", prefix: "+265" },
    { label: "Malaysia (+60)", prefix: "+60" },
    { label: "Maldives (+960)", prefix: "+960" },
    { label: "Mali (+223)", prefix: "+223" },
    { label: "Malta (+356)", prefix: "+356" },
    { label: "Mauritania (+222)", prefix: "+222" },
    { label: "Mauritius (+230)", prefix: "+230" },
    { label: "Mexico (+52)", prefix: "+52" },
    { label: "Moldova (+373)", prefix: "+373" },
    { label: "Monaco (+377)", prefix: "+377" },
    { label: "Mongolia (+976)", prefix: "+976" },
    { label: "Montenegro (+382)", prefix: "+382" },
    { label: "Morocco (+212)", prefix: "+212" },
    { label: "Mozambique (+258)", prefix: "+258" },
    { label: "Myanmar (+95)", prefix: "+95" },
    { label: "Namibia (+264)", prefix: "+264" },
    { label: "Nepal (+977)", prefix: "+977" },
    { label: "Netherlands (+31)", prefix: "+31" },
    { label: "New Zealand (+64)", prefix: "+64" },
    { label: "Nicaragua (+505)", prefix: "+505" },
    { label: "Niger (+227)", prefix: "+227" },
    { label: "Nigeria (+234)", prefix: "+234" },
    { label: "North Korea (+850)", prefix: "+850" },
    { label: "Norway (+47)", prefix: "+47" },
    { label: "Oman (+968)", prefix: "+968" },
    { label: "Pakistan (+92)", prefix: "+92" },
    { label: "Panama (+507)", prefix: "+507" },
    { label: "Papua New Guinea (+675)", prefix: "+675" },
    { label: "Paraguay (+595)", prefix: "+595" },
    { label: "Peru (+51)", prefix: "+51" },
    { label: "Philippines (+63)", prefix: "+63" },
    { label: "Poland (+48)", prefix: "+48" },
    { label: "Portugal (+351)", prefix: "+351" },
    { label: "Qatar (+974)", prefix: "+974" },
    { label: "Romania (+40)", prefix: "+40" },
    { label: "Russia (+7)", prefix: "+7" },
    { label: "Rwanda (+250)", prefix: "+250" },
    { label: "Saudi Arabia (+966)", prefix: "+966" },
    { label: "Senegal (+221)", prefix: "+221" },
    { label: "Serbia (+381)", prefix: "+381" },
    { label: "Sierra Leone (+232)", prefix: "+232" },
    { label: "Singapore (+65)", prefix: "+65" },
    { label: "Slovakia (+421)", prefix: "+421" },
    { label: "Slovenia (+386)", prefix: "+386" },
    { label: "Somalia (+252)", prefix: "+252" },
    { label: "South Africa (+27)", prefix: "+27" },
    { label: "South Korea (+82)", prefix: "+82" },
    { label: "South Sudan (+211)", prefix: "+211" },
    { label: "Spain (+34)", prefix: "+34" },
    { label: "Sri Lanka (+94)", prefix: "+94" },
    { label: "Sudan (+249)", prefix: "+249" },
    { label: "Suriname (+597)", prefix: "+597" },
    { label: "Sweden (+46)", prefix: "+46" },
    { label: "Switzerland (+41)", prefix: "+41" },
    { label: "Syria (+963)", prefix: "+963" },
    { label: "Taiwan (+886)", prefix: "+886" },
    { label: "Tajikistan (+992)", prefix: "+992" },
    { label: "Tanzania (+255)", prefix: "+255" },
    { label: "Thailand (+66)", prefix: "+66" },
    { label: "Togo (+228)", prefix: "+228" },
    { label: "Trinidad and Tobago (+1-868)", prefix: "+1-868" },
    { label: "Tunisia (+216)", prefix: "+216" },
    { label: "Turkey (+90)", prefix: "+90" },
    { label: "Turkmenistan (+993)", prefix: "+993" },
    { label: "Uganda (+256)", prefix: "+256" },
    { label: "Ukraine (+380)", prefix: "+380" },
    { label: "United Arab Emirates (+971)", prefix: "+971" },
    { label: "United Kingdom (+44)", prefix: "+44" },
    { label: "United States (+1)", prefix: "+1" },
    { label: "Uruguay (+598)", prefix: "+598" },
    { label: "Uzbekistan (+998)", prefix: "+998" },
    { label: "Venezuela (+58)", prefix: "+58" },
    { label: "Vietnam (+84)", prefix: "+84" },
    { label: "Yemen (+967)", prefix: "+967" },
    { label: "Zambia (+260)", prefix: "+260" },
    { label: "Zimbabwe (+263)", prefix: "+263" },
    { label: "Other (enter prefix)...", prefix: "other" }
];

function initPhonePrefix(opts) {
    var sel    = document.querySelector(opts.selectSel);
    var custom = document.querySelector(opts.customSel);
    var local  = document.querySelector(opts.localSel);
    var hidden = document.querySelector(opts.hiddenSel);
    var form   = document.querySelector(opts.formSel);
    if (!sel || !local || !hidden || !form) return;

    // Populate select
    PHONE_PREFIXES.forEach(function (c) {
        var opt = document.createElement('option');
        opt.value = c.prefix;
        opt.textContent = c.label;
        sel.appendChild(opt);
    });

    function syncHidden() {
        var localVal = local.value.trim();
        if (!localVal) { hidden.value = ''; return; }
        var prefix = sel.value === 'other'
            ? (custom ? custom.value.trim() : '')
            : sel.value;
        hidden.value = (prefix + localVal).trim();
    }

    // Show/hide custom prefix input and keep hidden in sync
    sel.addEventListener('change', function () {
        if (custom) {
            custom.style.display = sel.value === 'other' ? '' : 'none';
            if (sel.value === 'other') custom.focus();
        }
        syncHidden();
    });
    local.addEventListener('input', syncHidden);
    if (custom) custom.addEventListener('input', syncHidden);

    // Also sync on submit as a safety net
    form.addEventListener('submit', syncHidden);

    // On init: if hidden already has a value, restore the UI from it.
    // This preserves the phone number after a validation round-trip (server
    // re-sends model.PhoneNumber in the hidden field but the visible inputs are blank).
    var _existing = hidden.value.trim();
    if (_existing) {
        var _matched = false;
        for (var i = 0; i < PHONE_PREFIXES.length; i++) {
            var _p = PHONE_PREFIXES[i];
            if (_p.prefix !== 'other' && _existing.startsWith(_p.prefix)) {
                sel.value   = _p.prefix;
                local.value = _existing.slice(_p.prefix.length).trim();
                _matched    = true;
                break;
            }
        }
        if (!_matched) { local.value = _existing; }
        // hidden.value is already correct — do NOT call syncHidden()
    } else {
        syncHidden(); // no-op when both are empty; safe to call
    }
}
