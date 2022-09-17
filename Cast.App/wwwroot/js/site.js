// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

import Player from './player.js'
const player = new Player()

// Popover bindings
const $conversionButton = $('.conversion button')
const $popoverButton = $('.conversion [data-bs-toggle="popover"]')
const conversionPopover = new bootstrap.Popover($popoverButton)
$('body').on('click', '.conversion-state .stop-icon', (e) => {
    const targetId = $(e.target).attr('data-media-id')
    $.post(`conversion?handler=stopConversion`, { guid: targetId },
        content => {
            $(`[data-id="${targetId}"]`).replaceWith(content)
            $popoverButton.popover('hide')
        })
})

// Load/Refresh library
const $main = $('main')
const loadLibrary = () => {
    const md5 = $('#MediaMD5').val()
    if (!md5) $.get('library', data => $main.html(data))
    else $.get(`library?md5=${md5 ?? $('#MediaMD5').val()}`, data => {
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
            $.post(`conversion?handler=startConversion`, { guid: id }, content => {
                $(`[data-id="${id}"]`).html(content)
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
