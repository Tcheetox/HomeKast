
// Manage ChromeCast player
// https://github.com/castjs/castjs

import Castjs from './cast.js'

export default class Player {
    #isChrome = /Chrome/.test(navigator.userAgent) && /Google Inc/.test(navigator.vendor)

    #$playerBar = $('.media-player')
    #$currentTime = this.#$playerBar.find('.current')
    #$totalTime = this.#$playerBar.find('.total')
    #$subtitlesIcon = this.#$playerBar.find('img.subtitles')
    #$subtitlesMenu = this.#$playerBar.find('.subtitles.dropdown-menu')
    #$speaker = this.#$playerBar.find('img.speaker')
    #$castTitle = this.#$playerBar.find('.cast-title')
    #$playPause = this.#$playerBar.find('img.playPause')
    #$previous = this.#$playerBar.find('img.previous')
    #$next = this.#$playerBar.find('img.next')
    #$range = this.#$playerBar.find('.cast-progression input[type=range]')

    #cjs = null
    #currentMedia = null

    constructor() {
        if (this.#$playerBar.length === 0 || !this.#isChrome) return

        this.#cjs = new Castjs();
        if (!this.#cjs) return

        // Bind
        this.#cjs.on('available', () => {
            // Cast callback handlers
            this.#cjs.on('error', this.#onError)
            this.#cjs.on('event', (e) => {
                switch (e) {
                    case 'connect':
                        this.#onConnected(true)
                        break;
                    case 'disconnect':
                        this.#onConnected(false)
                        break;
                    case 'pause':
                        this.#onPlay(true)
                        break;
                    case 'playing':
                        this.#onPlay(false)
                        break;
                    case 'mute':
                        this.#onSpeaker(true)
                        break;
                    case 'unmute':
                        this.#onSpeaker(false)
                        break;
                    case 'end':
                        this.#onEnded()
                        break;
                    case 'timeupdate':
                        this.#$currentTime.html(this.#formatTimeSpan(this.#cjs.timePretty))
                        if (this.#cjs.time && this.#cjs.duration && this.#cjs.duration !== 0) {
                            const progressPercentage = parseInt((100 * this.#cjs.time) / this.#cjs.duration)
                            this.#$range.attr('value', progressPercentage)
                        }
                        break;
                }
            })

            // Icons and buttons action
            this.#$previous.on('click', () => this.#cjs.seek(0))
            this.#$next.on('click', () => this.#cjs.seek(100, true))
            this.#$range.on('change', e => this.#cjs.seek($(e.currentTarget).val(), true))
            this.#$playPause.on('click', () => {
                if (this.#cjs.paused) this.#cjs.play()
                else this.#cjs.pause()
            })
            this.#$speaker.on('click', () => {
                if (this.#cjs.muted) this.#cjs.unmute()
                else this.#cjs.mute()
            })
            this.#$playerBar.find('.dropdown-menu').on('click', '.dropdown-item', (e) => {
                const idx = parseInt($(e.currentTarget).attr('data-id'))
                this.#cjs.subtitle(idx)
                this.#updateSubtitles(idx)
                console.log(`> Subtitles (${idx})`)
            })
        })
    }

    cast = (host, data) => {
        if (!data || !host) return
        const options = {
            poster: host + data.metadata.imageUrl,
            title: data.name,
            subtitles: data.subtitles.map((s, i) => ({
                active: s.active,
                label: s.displayLabel,
                src: `${host}library?handler=mediaSubtitles&guid=${data.id}&idx=${i}`
            }))
        }
        this.#currentMedia = data
        this.#cjs?.cast(`${host}library?handler=mediaStream&guid=${data.id}`, options)
    }

    subtitles = args => this.#cjs?.subtitle(args)

    #formatTimeSpan = v => {
        const trimmed = v.substring(1)
        if (trimmed.includes('.')) return trimmed.split('.')[0]
        return trimmed
    }

    #updateTitle = title => {
        if (!title || title === '') title = this.#cjs.title
        if (title !== this.#$castTitle.html())
            this.#$castTitle.html(title)
    }

    #updateTotalTime = () => {
        if (!this.#currentMedia && !this.#cjs.durationPretty)
            return

        const formattedValue = this.#formatTimeSpan(this.#currentMedia?.length ?? this.#cjs?.durationPretty)
        if (this.#$totalTime.html() !== formattedValue)
            this.#$totalTime.html(formattedValue)
    }

    #updateSubtitles = idx => {
        if (!this.#cjs.subtitles) return
        const content = this.#cjs.subtitles.map(s => {
            const sIdx = parseInt(s.src.slice(-1))
            const active = (!Number.isInteger(idx) && s.active) || (Number.isInteger(idx) && sIdx === idx)
                ? '&emsp;&#10003;'
                : ''
            return `<li><button class="dropdown-item" type="button" data-id="${sIdx}">${s.label}${active}</button></li>`
        })
        if (!content || content.length === 0) {
            if (!this.#$subtitlesIcon.hasClass('disabled')) this.#$subtitlesIcon.addClass('disabled')
        } else {
            if (this.#$subtitlesIcon.hasClass('disabled')) this.#$subtitlesIcon.removeClass('disabled')
        }
        this.#$subtitlesMenu.html(content)
    }

    #updateAll = () => {
        this.#updateTitle()
        this.#updateTotalTime()
        this.#updateSubtitles()
    }

    #onConnected = (active) => {
        if (active) {
            this.#updateAll()
            this.#$playerBar.show()
            console.log('> Connected')
        } else {
            this.#currentMedia = null
            this.#updateTitle('Disconnected')
            this.#$totalTime.html('0:00:00')
            this.#$playerBar.hide()
            console.log('> Disconnected')
        }
    }

    #onPlay = (pause) => {
        if (pause) {
            this.#$playPause.attr('src', '/media/play.svg')
            console.log('> Paused')
        } else {
            this.#updateAll()
            this.#$playPause.attr('src', '/media/pause.svg')
            console.log('> Playing')
        }
    }

    #lastChangedAt = null
    #onSpeaker = muted => {
        const now = new Date()
        if (this.#lastChangedAt && (now - this.#lastChangedAt) < 100) // Mini-hack because the event is fired multiple times for a single action
            return
        if (muted) {
            this.#$speaker.attr('src', '/media/muted.svg')
            console.log('> Muted')
        } else {
            this.#$speaker.attr('src', '/media/unmuted.svg')
            console.log('> Unmuted')
        }
        this.#lastChangedAt = now
    }

    #onEnded = () => {
        console.log('> Ended')
        this.#cjs.disconnect()
    }

    #onError = (e) => {
        this.#updateTitle('Error')
        console.log('> Errored')
        console.error(e)
    }
}