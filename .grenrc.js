module.exports = {
    "dataSource": "milestones",
    "prefix": "v",
        "template": {
        "issue": "- [{{text}}]({{url}}) {{name}}"
    },
    "groupBy": {
        "Enhancements:": ["enhancement", "internal"],
        "Bug Fixes:": ["bug"]
    },
    "milestoneMatch": "Release {{tag_name}}",
    "changelogFilename": "CHANGELOG.md",
    "ignoreIssuesWith": ["duplicate", "help wanted", "invalid", "wontfix"]
}