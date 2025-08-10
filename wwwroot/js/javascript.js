//this is the javascript for the navigation bar
const toggleButton = document.querySelector('.toggle');
const navigation = document.querySelector('.navigation');

if (toggleButton && navigation) {
    toggleButton.addEventListener('click', function() {
        navigation.classList.toggle('active');
    });
}

//this is the javascript for the customised presentation
//we are using the local storage for the background color but not for the text size
let button1 = document.querySelector('#btn1');
let button2 = document.querySelector('#btn2');
let button3 = document.querySelector('#btn3');
let button4 = document.querySelector('#btn4');

// Load saved preferences on page load
document.addEventListener('DOMContentLoaded', () => {
    const savedFontSize = localStorage.getItem('fontSize');
    const backgroundColor = localStorage.getItem('backgroundColor');
    const textColor = localStorage.getItem('textColor');
    
    if (savedFontSize) {
        document.querySelectorAll('body *').forEach(element => {
            if (element.children.length === 0 && element.textContent.trim()) {
                element.style.fontSize = savedFontSize;
            }
        });
    }
    const header = document.querySelector('.head');
    const footer = document.querySelector('footer');
    if (backgroundColor && textColor && header) {
        header.style.backgroundColor = backgroundColor;
        header.style.color = textColor;
        if (footer) {
            footer.style.backgroundColor = backgroundColor;
            footer.style.color = textColor;
        }
    }
});

function increaseFontSize() {
    const elements = document.querySelectorAll('body *:not(#btn1):not(#btn2):not(#btn3):not(#btn4):not(.container):not(.card_container):not(.card_article):not(.card_article2):not(.card_article3):not(.card_article4):not(.top-trending):not(.top-trending2):not(.top-trending3):not(.event-listing):not(.location-button1):not(.location-links-container):not(.location-link)'
);
    elements.forEach(element => {
        if (element.children.length === 0 && element.textContent.trim()) {
            const currentFontSize = window.getComputedStyle(element).fontSize;
            const newFontSize = Math.min(parseFloat(currentFontSize) + 2, 24) + 'px';
            element.style.fontSize = newFontSize;
            localStorage.setItem('fontSize', newFontSize);
        }
    });
}

function decreaseFontSize() {
    const elements = document.querySelectorAll('body *:not(#btn1):not(#btn2):not(#btn3):not(#btn4):not(.container):not(.card_container):not(.card_article):not(.card_article2):not(.card_article3):not(.card_article4):not(.top-trending):not(.top-trending2):not(.top-trending3):not(.event-listing):not(.location-button1):not(.location-links-container):not(.location-link)'
);
    elements.forEach(element => {
        if (element.children.length === 0 && element.textContent.trim()) {
            const currentFontSize = window.getComputedStyle(element).fontSize;
            const newFontSize = Math.max(parseFloat(currentFontSize) - 2, 10) + 'px';
            element.style.fontSize = newFontSize;
            localStorage.setItem('fontSize', newFontSize);
        }
    });
}

function lightBackground() {
    const header = document.querySelector('.head');
    const footer = document.querySelector('footer');
    header.style.backgroundColor = "#E5D0CC";
    header.style.color = "black";
    if (footer) {
        footer.style.backgroundColor = "#E5D0CC";
        footer.style.color = "black";
    }
    localStorage.setItem('backgroundColor', "#E5D0CC");
    localStorage.setItem('textColor', "black");
}

function darkBackground() {
    const header = document.querySelector('.head');
    const footer = document.querySelector('footer');
    header.style.backgroundColor = "#BFACB5";
    header.style.color = "white";
    if (footer) {
        footer.style.backgroundColor = "#BFACB5";
        footer.style.color = "white";
    }
    localStorage.setItem('backgroundColor', "#BFACB5");
    localStorage.setItem('textColor', "white");
}

if (button1) button1.addEventListener("click", increaseFontSize);
if (button2) button2.addEventListener("click", decreaseFontSize);
if (button3) button3.addEventListener("click", darkBackground);
if (button4) button4.addEventListener("click", lightBackground);


//this is the javascript to apply chnages in case of reloaded page using the DOMContentLoaded
window.addEventListener('DOMContentLoaded', () => {
    //first the font size
    const savedFontSize = localStorage.getItem('fontSize');
    if (savedFontSize) {
        const elements = document.querySelectorAll('*:not(#btn1):not(#btn2):not(#btn3):not(#btn4)');
        elements.forEach(element => element.style.fontSize = savedFontSize);
    }

    //then the background colour
    const savedBackgroundColor = localStorage.getItem('backgroundColor');
    const savedTextColor = localStorage.getItem('textColor');
    if (savedBackgroundColor && savedTextColor) {
        const header = document.querySelector('.head');
        const footer = document.querySelector('footer');
        header.style.backgroundColor = savedBackgroundColor;
        header.style.color = savedTextColor;
        if (footer) {
            footer.style.backgroundColor = savedBackgroundColor;
            footer.style.color = savedTextColor;
        }
    }
});

//add this part is for the drop down nav bar of the customisation part 
const setting = document.querySelector('.logo2');
const settingbutton = document.getElementById('div6');

if (setting && settingbutton) {
    setting.addEventListener('click', function() {
        if (settingbutton.style.display === "none" || settingbutton.style.display === "") {
            settingbutton.style.display = "flex"; 
        } else {
            settingbutton.style.display = "none";
        }
    });

    document.addEventListener('click', function(event) {
        if (!setting.contains(event.target) && !settingbutton.contains(event.target)) {
            settingbutton.style.display = "none"; 
        }
    });
}

//this is for the location button part: 
// Location Button Toggle
document.addEventListener("DOMContentLoaded", () => {
    const locButton = document.querySelector('.circle-btn');
    const locationContainer = document.querySelector('.location-links-container');
    if (locButton && locationContainer) {
        locButton.addEventListener("click", function (e) {
            locButton.classList.toggle("expanded");
            locationContainer.classList.toggle("expanded");
            e.stopPropagation(); // Prevent event bubbling
        });

        document.addEventListener("click", function (e) {
            if (!locButton.contains(e.target) && !locationContainer.contains(e.target)) {
                locButton.classList.remove("expanded");
                locationContainer.classList.remove("expanded");
            }
        });
    }
});

// Show Events for Selected Location
document.addEventListener("DOMContentLoaded", () => {
    // Get current location from URL parameters if available
    const urlParams = new URLSearchParams(window.location.search);
    const location = urlParams.get('location') || '';
    
    // Highlight the current location in the navigation if it exists
    if (location) {
        const locationLinks = document.querySelectorAll('.location-link');
        locationLinks.forEach(link => {
            const linkHref = link.getAttribute('href');
            if (linkHref && linkHref.includes(location)) {
                link.style.fontWeight = 'bold';
                link.style.color = '#522B5B';
            }
        });
    }
});

// Handle event card interactions
document.addEventListener("DOMContentLoaded", () => {
    // Apply hover effects to event cards
    const eventCards = document.querySelectorAll('.event-card');
    if (eventCards.length > 0) {
        eventCards.forEach(card => {
            card.addEventListener('mouseenter', function() {
                this.style.transform = 'translateY(-10px)';
                this.style.boxShadow = '0 15px 30px rgba(0, 0, 0, 0.2)';
            });
            
            card.addEventListener('mouseleave', function() {
                this.style.transform = 'translateY(0)';
                this.style.boxShadow = '0 5px 15px rgba(0, 0, 0, 0.1)';
            });
        });
    }
    
    // Handle event image galleries in event detail page
    const imageSlides = document.querySelectorAll('.image-slide');
    if (imageSlides.length > 1) {
        let currentSlide = 0;
        
        // Add navigation controls
        const slideContainer = document.querySelector('.event-images');
        if (slideContainer) {
            const prevButton = document.createElement('button');
            prevButton.className = 'slide-nav prev';
            prevButton.innerHTML = '&lt;';
            
            const nextButton = document.createElement('button');
            nextButton.className = 'slide-nav next';
            nextButton.innerHTML = '&gt;';
            
            slideContainer.appendChild(prevButton);
            slideContainer.appendChild(nextButton);
            
            // Show only the first slide initially
            imageSlides.forEach((slide, index) => {
                if (index !== 0) {
                    slide.style.display = 'none';
                }
            });
            
            // Navigation handlers
            prevButton.addEventListener('click', () => {
                imageSlides[currentSlide].style.display = 'none';
                currentSlide = (currentSlide - 1 + imageSlides.length) % imageSlides.length;
                imageSlides[currentSlide].style.display = 'block';
            });
            
            nextButton.addEventListener('click', () => {
                imageSlides[currentSlide].style.display = 'none';
                currentSlide = (currentSlide + 1) % imageSlides.length;
                imageSlides[currentSlide].style.display = 'block';
            });
        }
    }
});

document.addEventListener("DOMContentLoaded", function () {
    // Handle form submission for user information if present
    const userForm = document.querySelector(".div form");
    if (userForm) {
        userForm.addEventListener("submit", function (event) {
            event.preventDefault(); // Prevent actual form submission
            alert("Form submitted. Information received.");
            userForm.reset(); // Clear the form fields after submission
        });
    }

    // Handle form submission for feedback if present
    const feedbackForm = document.querySelector("#form2");
    if (feedbackForm) {
        feedbackForm.addEventListener("submit", function (event) {
            event.preventDefault(); // Prevent actual form submission
            alert("Feedback submitted. Information received.");
            feedbackForm.reset(); // Clear the feedback form after submission
        });
    }

    // Add CSS for image navigation controls
    const style = document.createElement('style');
    style.textContent = `
        .event-images {
            position: relative;
        }
        .slide-nav {
            position: absolute;
            top: 50%;
            transform: translateY(-50%);
            background: rgba(0,0,0,0.5);
            color: white;
            border: none;
            width: 40px;
            height: 40px;
            border-radius: 50%;
            font-size: 20px;
            cursor: pointer;
            z-index: 10;
        }
        .slide-nav.prev {
            left: 10px;
        }
        .slide-nav.next {
            right: 10px;
        }
        .slide-nav:hover {
            background: rgba(0,0,0,0.8);
        }
    `;
    document.head.appendChild(style);
});


//dynamicaly populate the events in the home page
fetch('/data/events.json')
    .then(response => response.json())
    .then(data => {
        // Randomly shuffle the events array
        const shuffledEvents = data.sort(() => 0.5 - Math.random());

        // Select the first 3 events
        const selectedEvents = shuffledEvents.slice(0, 3);

        // Get the container where the events will be displayed
        const eventList = document.querySelector('.event-list');

        selectedEvents.forEach((event, index) => {
            const eventItem = document.createElement('div');
            eventItem.className = 'event-item';
            eventItem.id = `item${index + 1}`;

            const eventTitle = document.createElement('h3');
            eventTitle.textContent = event.title;

            const eventDate = document.createElement('p');
            eventDate.textContent = event.date;

            eventItem.appendChild(eventTitle);
            eventItem.appendChild(eventDate);
            eventList.appendChild(eventItem);

            //add a keyframe that will display 3 event's images based on the title display from the JSON file
            if (event.images && event.images.length > 0) {
                const backgroundChange = `backgroundChange${index + 1}`;
                const keyframes = `
                    @keyframes ${backgroundChange} {
                        ${event.images.map((img, i) => `${(i / (event.images.length - 1)) * 100}% { background-image: url('${img}'); }`).join(' ')}
                    }
                `;

                const styleSheet = document.createElement('style');
                styleSheet.textContent = keyframes;
                document.head.appendChild(styleSheet);

                eventItem.style.animation = `${backgroundChange} 10s ease-in-out infinite`;
                eventItem.style.backgroundSize = 'cover';
                eventItem.style.backgroundPosition = 'center';
            }
        });
    })
    .catch(error => console.error('Error fetching JSON:', error));


//dynamically populate the event in london structure in the home page
fetch('/data/events2.json')
    .then(response => response.json())
    .then(events => {
        // Shuffle events and select the first 3
        const shuffledEvents = events.sort(() => Math.random() - 0.5);
        const randomEvents = shuffledEvents.slice(0, 3);

        // Map the random events to the respective labels
        randomEvents.forEach((event, index) => {
            const inputId = `f${index + 1}`; // Matches input IDs f1, f2, f3
            const label = document.querySelector(`label[for="${inputId}"]`);

            if (label) {
                // Set background to cover the label
                label.style.backgroundImage = `url('${event.image}')`;
                label.style.backgroundSize = 'cover'; // Ensures image covers the entire div
                label.style.backgroundPosition = 'center'; // Centers the image
                label.style.backgroundRepeat = 'no-repeat'; // Prevent repeating
                label.style.filter = 'grayscale(45%)'; // Apply grayscale effect

                // Update the title and date inside the description element
                const description = label.querySelector('.description');
                if (description) {
                    description.querySelector('h3').textContent = event.title;
                    description.querySelector('p').textContent = event.date;
                }
            }
        });
    })
    .catch(error => console.error('Error fetching JSON:', error));


document.addEventListener("DOMContentLoaded", function () {
    // Handle form submission for user information
    const userForm = document.querySelector(".div form");
    userForm.addEventListener("submit", function (event) {
      event.preventDefault(); // Prevent actual form submission
      alert("Form submitted. Information received.");
      userForm.reset(); // Clear the form fields after submission
    });
  
    // Handle form submission for feedback
    const feedbackForm = document.querySelector("#form2");
    feedbackForm.addEventListener("submit", function (event) {
      event.preventDefault(); // Prevent actual form submission
      alert("Feedback submitted. Information received.");
      feedbackForm.reset(); // Clear the feedback form after submission
    });
  });
  

