module.exports = {
    "dataSource": "milestones",
    "prefix": "v",
	"template": {
        issue: "- [{{text}}]({{url}}) {{name}}",
		release: function (placeholders) {
			var parts = placeholders.date.split("/");
			var dt = new Date(parseInt(parts[2], 10),
                  parseInt(parts[1], 10) - 1,
                  parseInt(parts[0], 10));
            return '## ' + placeholders.release + ' (' + dt.toISOString().substring(0, 10).replace(/-/g,"/") +")\n" + placeholders.body;
		}
    },
    "groupBy": {
        "Enhancements:": ["enhancement", "internal"],
        "Bug Fixes:": ["bug"]
    },
    "milestoneMatch": "Release {{tag_name}}",
    "changelogFilename": "CHANGELOG.md",
    "ignoreIssuesWith": ["duplicate", "help wanted", "invalid", "wontfix"]
}