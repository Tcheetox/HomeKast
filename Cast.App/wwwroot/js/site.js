// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// TODO: bind properly title name JS
// TODO: bind seek +- buttons
// TODO: make this shit pretty

import Player from './player.js'
const player = new Player()

// Antiforgery
const $csrf = $('input[name="__RequestVerificationToken"]')

// Popover bindings
const $conversionButton = $('.conversion button')
const $popoverButton = $('.conversion [data-bs-toggle="popover"]')
const conversionPopover = new bootstrap.Popover($popoverButton)
$('body').on('click', '.conversion-state .stop-icon', (e) => {
    const targetId = $(e.target).attr('data-media-id')
    $.ajax({
        url: 'conversion?handler=stopConversion',
        type: 'post',
        data: { guid: targetId },
        headers: { RequestVerificationToken: $csrf.val() }
    }).done(data => {
        $(`[data-id="${targetId}"]`).replaceWith(data)
        $popoverButton.popover('hide')
    })
})

// Load/Refresh library
const $main = $('main')
const loadLibrary = () => {
    const md5 = $('.md5').val()
    $.get(md5 ? `library?md5=${md5}` : 'library', data => {
        if (data) $main.html(data)
    })
}
setInterval(loadLibrary, 1000)

// Media trigger
$('main').on('click', '.library .media', (e) => {
    if (!player) return
    const $media = $(e.currentTarget)
    const status = $media.data('status')
    switch (status) {
        case 'playable':
            player.cast(`${$('.library').data('host')}library?handler=mediaStream&guid=${$media.data('id')}`, {
                title: $media.data('title')
            })
            break;
        case 'unplayable':
            const id = $media.data('id')
            $.ajax({
                url: 'conversion?handler=startConversion',
                type: 'post',
                data: { guid: id },
                headers: { RequestVerificationToken: $csrf.val() },
            }).done(data => {
                $(`[data-id="${id}"]`).html(data)
                $('#MediaMD5').val(null)
            })
            break;
        default:
            console.warn(`> No action defined for media state: ${status}`)
    }
})

// Check for current media being converted
let previouslyConverting = false
const checkConverting = () => {
    $.get('/library?handler=mediaConversionState', (data, status) => {
        const currentlyConverting = (data?.queueLength > 0) ?? false
        // Update conversion button visibility
        if (previouslyConverting !== currentlyConverting)
            $conversionButton.toggle()

        // Adjust popover content
        const $conversionState = $('.conversion-state')
        const $progressBar = $conversionState.find('.progress-bar')
        if (data) {
            $conversionState.find('.stop-icon').attr('data-media-id', data.id)
            const progress = `${data.progress?.percent ?? 0}%`
            $progressBar.html(progress)
            $progressBar.css('width', progress)
            $conversionState.find('.mediaTitle').html(data.name)
            $('.queueLength').html(`${data.queueLength} item(s)`)
        } else {
            $conversionState.find('.stop-icon').attr('data-media-id', null)
            $progressBar.html('0%')
            $progressBar.css('width', '0%')
            $conversionState.find('.mediaTitle').html('')
            $('.queueLength').html('0 item(s)')
        }

        // Reload entire library when done converting
        if (previouslyConverting && !currentlyConverting) {
            $popoverButton.popover('hide')
            loadLibrary()
        }
            
        // Store new state and go again...
        previouslyConverting = currentlyConverting
        if (status === "success" || status == "nocontent")
            setTimeout(checkConverting, 300)
    })
}
const $conversionPlaceholder = $('.conversion-placeholder')
if ($conversionPlaceholder.length > 0) checkConverting()
