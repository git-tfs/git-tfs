* Pick up labels that don't have the branch folder specifically listed (#1125)
* ignore-not-init-branches value is not longer case sensitive (#1189 by @Laibalion)
* ignore-not-init-branches now configurable via commandline flag
* ignore-branches-regex to configure a filter regex to exclude branches during cloning or fetching with branches
* ignore changesets that the TFS user has no read access to with the `--ignore-restricted-changesets` option
* fixed bug in detecting of external repositories from the default repository (#1194 by @Laibalion)
* Fix critical bug in merge parent changeset lookup (#1195 by @Laibalion)
* handle "TF14098: Access Denied" exceptions (#1176 by @larsxschneider )