# Bruno repository workflow

Bruno is an independent Windows desktop companion for the owner's fawn pug.
Build with build.ps1 [-Dev]; verify with tools/Verify.ps1. Ordinary builds use
only app/ and art/, with no parent checkout, original photos or Python dependency.

When the local umbrella exists, first read ../AGENTS.md, ../workspace.json,
../shared/README.md and planning/PLAN.md. Canonical reusable framework edits belong
in shared/framework and must be explicitly synced with shared/tools/Sync-Framework.ps1
-Pet Bruno before checking this repository. Builds never sync automatically.

Preserve the owner-approved PNG bytes, action timings, scale and horizontal anchors.
Current floor support is the seated butt and lying chest/elbows; paws hang over.
Owner passed the corrected smooth-transition v3 cycle on 2026-11-18. Version
0.1.3 uses its exact five-key posture routes and selected new PNGs; the legacy
sit-lay-1 remains preserved but is no longer in the active sit/lie routes.
Approval receipt: art/approvals/approved-smooth-transitions-v3.json. Complete local
snapshot: draft-assets/approved-smooth-transitions-v3/. Do not restore the rejected
out-of-order crouch sequence or infer new desktop/installer approvals from this pass.
Runtime registration lives in art/manifest.json. Small approval records are in
art/approvals; complete immutable snapshots and internal plans remain local-only.
Do not copy another pet's anatomy, settings, mutex, startup key or release identity.

Never publish original archives, references, draft artwork, internal planning,
working binaries or credentials. Release assets are uploaded explicitly; preserve
existing approved artwork and local media. Runtime art must remain tracked.

Current public target: BurtFablehoff/Bruno. The owner authorized publishing source,
release notes, sample image and installer on 2026-11-14. Release 0.1.2 uses the
verified existing production EXE and installer bytes. No local installer execution
is authorized by publication. The updater remains disabled in this release.

Startup is opt-in under the Bruno Run value. Production and development builds
have isolated settings/mutex/product identities. Automated verification stays
headless and must not change owner settings/startup or launch the companion.
Installer packaging requires an explicitly supplied external Inno compiler and
must reject developer/other-product binaries. No tools are downloaded automatically.

Use Still working for progress and Done for completed requested work. Do not infer
unconfirmed mixed-DPI, startup or installer interaction from a visual placement pass.
