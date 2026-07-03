// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {

    $("#selectAll").on("change", function () {
        $(".row-check").prop("checked", this.checked).trigger("change");
    });

    $(document).on("change", ".row-check", function () {
        $("#selectAll").prop("checked",
            $(".row-check:checked").length === $(".row-check").length);
    });

    $("#hblDropdown").on("change", function () {
        const selected = $(this).find(":selected");
        $("#etaInput").val(selected.attr("data-eta") || "");
        $("#containerInput").val(selected.attr("data-container") || "");
    });

    $(document).on("change", ".row-check", function () {

        const row = $(this).closest("tr");
        const outstanding = parseFloat($(this).data("outstanding")) || 0;

        if ($(this).is(":checked")) {
            row.find(".pd-input").val(outstanding).attr("max", outstanding);
            row.find(".pd-text").hide();
            row.find(".pd-input").show();
            row.addClass("table-warning");
            row.find(".btn-tag").show();
        } else {
            row.find(".pd-text").show();
            row.find(".pd-input").hide();
            row.removeClass("table-warning table-danger");
            row.find(".btn-tag").hide();
        }

        toggleTagButtons();
    });

    $(document).on("input", ".pd-input", function () {

        const row = $(this).closest("tr");
        const max = parseFloat($(this).attr("max")) || 0;
        const value = parseFloat($(this).val()) || 0;

        if (value > max) {
            alert("Partial Delivered cannot exceed Outstanding Quantity!");
            $(this).val(max);
            row.addClass("table-danger");
        } else {
            row.removeClass("table-danger");
        }
    });

    $(document).on("click", ".btn-tag", function (e) {

        const row = $(this).closest("tr");

        const outstanding = parseFloat(row.find(".row-check").data("outstanding")) || 0;
        const partial = parseFloat(row.find(".pd-input").val()) || 0;

        if (partial > outstanding) {
            alert("Partial Delivered cannot be greater than Outstanding Quantity!");
            row.addClass("table-danger");
            row.find(".pd-input").focus();
            e.preventDefault();
        }
    });

    function toggleTagButtons() {
        $(".btn-tag").hide();

        $(".row-check:checked").each(function () {
            $(this).closest("tr").find(".btn-tag").show();
        });
    }

    setTimeout(() => {
        $(".alert").fadeOut("slow");
    }, 4000);

});