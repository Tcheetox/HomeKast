// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// TODO: bind properly title name JS
// TODO: bind seek +- buttons
// TODO: make this shit pretty
// TODO: all the bindings

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
        if (!data) return
        $(`[data-id="${targetId}"]`).replaceWith(data)
        $popoverButton.popover('hide')
    })
})

// Load/Refresh library
const $main = $('main')
const loadLibrary = callback => {
    const md5 = $('.md5').val()
    $.get(md5 ? `library?md5=${md5}` : 'library')
        .done((data) => {
            if (data) $main.html(data)
            if (callback) callback()
        })
}

(function selfReloadLibrary() {
    const every = 1000
    setTimeout(() => loadLibrary(selfReloadLibrary), every)
})()

// Media trigger
$('main').on('click', '.library .media', (e) => {
    const $media = $(e.currentTarget)
    const status = $media.data('status')
    const id = $media.data('id')
    switch (status) {
        case 'playable':
            $.ajax({
                url: 'library?handler=media',
                type: 'get',
                data: { guid: id }
            }).done(data => {   
                const host = $('.library').data('host')
                const options = {
                    poster: host + data.metadata.imageUrl,
                    title: data.name,
                    subtitles: data.subtitles.map((s, i) => ({
                        active: s.active,
                        label: s.displayLabel,
                        src: `${host}library?handler=mediaSubtitles&guid=${id}&idx=${i}`
                    }))
                }
                player.cast(`${host}library?handler=mediaStream&guid=${id}`, options)
            })
            break;
        case 'missingsubtitles':
        case 'unplayable':
            $.ajax({
                url: 'conversion?handler=startConversion',
                type: 'post',
                data: { guid: id },
                headers: { RequestVerificationToken: $csrf.val() },
            }).done(data => {
                if (!data) return
                $(`[data-id="${id}"]`).replaceWith(data)
                $('.md5').val(null)
            })
            break;
        default:
            console.warn(`> No action defined for media state: ${status}`)
    }
})

// Check for current media being converted
let previouslyConverting = false
const checkConverting = () => {
    $.get('/conversion?handler=mediaConversionState', (data, status) => {
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
            $('.popover-header .target').html(data.target === 2 ? 'Extracting...' : 'Converting...')
        } else {
            $conversionState.find('.stop-icon').attr('data-media-id', null)
            $progressBar.html('0%')
            $progressBar.css('width', '0%')
            $conversionState.find('.mediaTitle').html('')
            $('.queueLength').html('0 item(s)')
            $('.popover-header .target').html('Converting...')
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
